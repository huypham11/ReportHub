using System.Collections.Concurrent;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RHDomain.Controllers;

namespace RHInfrastructure.Controllers;

/// <summary>
/// Load controller khai báo bằng file .cs, tổ chức theo LOẠI (không theo controller):
///   Controllers/
///     Filter/{code}.cs   - bắt buộc: field lọc + bố cục (View) + Processing (gọi proc lấy data)
///     Grid/{code}.cs     - optional: cột hiển thị
/// {code} (không có phần mở rộng) chính là mã controller, không cần khai báo lại trong code.
/// File Filter/{code}.cs và Grid/{code}.cs phải cùng khai báo 1 partial class trùng tên.
/// </summary>
public class ControllerLoader
{
    private readonly string _folder;
    private string FilterFolder => Path.Combine(_folder, "Filter");
    private string GridFolder => Path.Combine(_folder, "Grid");

    private readonly ConcurrentDictionary<string, CompiledController> _cache = new();

    // Lỗi compile/load gần nhất theo từng code - để FE/API biết ngay có report nào đang lỗi,
    // thay vì chỉ log ra console (dev không nhìn thấy) rồi âm thầm giữ bản cache cũ.
    private readonly ConcurrentDictionary<string, string> _lastErrors = new();

    public ControllerLoader(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(FilterFolder);
        Directory.CreateDirectory(GridFolder);
    }

    public IReadOnlyCollection<CompiledController> All => _cache.Values.ToList();

    public IReadOnlyDictionary<string, string> LastErrors => _lastErrors;

    public CompiledController? Get(string code)
        => _cache.TryGetValue(code, out var c) ? c : null;

    public List<string> LoadAll()
    {
        var errors = new List<string>();
        var codes = Directory.GetFiles(FilterFolder, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!);

        foreach (var code in codes)
        {
            try
            {
                LoadCode(code);
                _lastErrors.TryRemove(code, out _);
            }
            catch (Exception ex)
            {
                errors.Add($"{code}: {ex.Message}");
                _lastErrors[code] = ex.Message;
            }
        }
        return errors;
    }

    /// <summary>
    /// Suy ra code từ đường dẫn file vừa thay đổi (dùng cho FileSystemWatcher), rồi load lại.
    /// Không throw ra ngoài - lỗi được lưu vào LastErrors để tra cứu qua API thay vì làm sập
    /// tiến trình watcher.
    /// </summary>
    public void ReloadFromChangedFile(string changedFilePath)
    {
        var code = Path.GetFileNameWithoutExtension(changedFilePath);
        if (string.IsNullOrEmpty(code)) return;

        try
        {
            LoadCode(code);
            _lastErrors.TryRemove(code, out _);
        }
        catch (Exception ex)
        {
            _lastErrors[code] = ex.Message;
        }
    }

    public void LoadCode(string code)
    {
        var filterFile = Path.Combine(FilterFolder, code + ".cs");
        if (!File.Exists(filterFile))
        {
            // File Filter đã bị xoá -> bỏ controller này khỏi cache luôn.
            _cache.TryRemove(code, out _);
            return;
        }

        var gridFile = Path.Combine(GridFolder, code + ".cs");
        var files = new List<string> { filterFile };
        if (File.Exists(gridFile)) files.Add(gridFile);

        var syntaxTrees = files
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f))
            .ToList();

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "Dyn_" + Guid.NewGuid().ToString("N"),
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errText = string.Join("\n", result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            throw new InvalidOperationException($"Compile error:\n{errText}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var loadContext = new AssemblyLoadContext("controller_" + code, isCollectible: true);
        var assembly = loadContext.LoadFromStream(ms);

        var type = assembly.GetTypes()
            .First(t => typeof(ControllerDefinitionBase).IsAssignableFrom(t) && !t.IsAbstract);

        var instance = (ControllerDefinitionBase)Activator.CreateInstance(type)!;

        var filterBuilder = new FieldBuilder();
        instance.ConfigureFilter(filterBuilder);

        var gridBuilder = new FieldBuilder();
        instance.ConfigureGrid(gridBuilder);

        var viewBuilder = new ViewBuilder();
        instance.ConfigureView(viewBuilder);

        var fields = new FieldBuilder();
        fields.Filters.AddRange(filterBuilder.Filters);
        fields.Grids.AddRange(gridBuilder.Grids);
        fields.CheckingHooks.AddRange(filterBuilder.CheckingHooks);
        fields.SetProcessingHook(filterBuilder.ProcessingHook);

        _cache[code] = new CompiledController
        {
            Code = code,
            Definition = instance,
            Fields = fields,
            View = viewBuilder
        };
    }
}
