namespace RHDomain.Controllers;

public enum FieldType { Date, Int, String, Number }

public class FilterFieldDef
{
    public string Field { get; set; } = "";
    public string Label { get; set; } = "";
    public FieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? LookupSource { get; set; }

    // Bất đồng bộ để cho phép default value lấy giá trị hệ thống từ SQL Server (ctx.CallProcedure),
    // không chỉ tính toán thuần C#.
    public Func<ExecutionContext, Task<object?>>? DefaultValueFactory { get; set; }
}

public class GridFieldDef
{
    public string Field { get; set; } = "";
    public string Label { get; set; } = "";
    public FieldType Type { get; set; }
    public string? Format { get; set; }
}

public class FilterFieldBuilder
{
    private readonly FilterFieldDef _def;
    public FilterFieldBuilder(FilterFieldDef def) => _def = def;

    public FilterFieldBuilder Required()
    {
        _def.IsRequired = true;
        return this;
    }

    public FilterFieldBuilder AsLookup(string sourceCode)
    {
        _def.LookupSource = sourceCode;
        return this;
    }

    // Default value tính thuần C# (không chạm DB), VD: new DateTime(...), ctx.CurrentUser.BranchId.
    public FilterFieldBuilder DefaultValue(Func<ExecutionContext, object?> factory)
    {
        _def.DefaultValueFactory = ctx => Task.FromResult(factory(ctx));
        return this;
    }

    // Default value cần lấy giá trị hệ thống từ SQL Server - dùng ctx.CallProcedure/ctx.QuerySql bên trong.
    public FilterFieldBuilder DefaultValue(Func<ExecutionContext, Task<object?>> factory)
    {
        _def.DefaultValueFactory = factory;
        return this;
    }
}

public class GridFieldBuilder
{
    private readonly GridFieldDef _def;
    public GridFieldBuilder(GridFieldDef def) => _def = def;

    public GridFieldBuilder Format(string? format)
    {
        _def.Format = format;
        return this;
    }
}

/// <summary>
/// Nơi khai báo field - dùng chung cho cả ConfigureFilter và ConfigureGrid, đúng tinh thần
/// FBO: <field> là thẻ dùng chung cho cả Filter XML lẫn Grid XML, không phải khái niệm riêng
/// của "Controller" (khái niệm đó đã bỏ từ lâu, đặt tên theo Controller không còn hợp lý).
/// </summary>
public class FieldBuilder
{
    public List<FilterFieldDef> Filters { get; } = new();
    public List<GridFieldDef> Grids { get; } = new();
    public List<Func<ExecutionContext, Task>> CheckingHooks { get; } = new();

    // Processing = nơi thực sự lấy data (gọi proc...), gắn ở Filter - giống command
    // event="Processing" của FBO. Chỉ nên có 1 hook Processing cho mỗi controller.
    public Func<ExecutionContext, Task>? ProcessingHook { get; private set; }

    public FilterFieldBuilder Filter(string name, string label, FieldType type)
    {
        var def = new FilterFieldDef { Field = name, Label = label, Type = type };
        Filters.Add(def);
        return new FilterFieldBuilder(def);
    }

    public GridFieldBuilder Grid(string name, string label, FieldType type)
    {
        var def = new GridFieldDef { Field = name, Label = label, Type = type };
        Grids.Add(def);
        return new GridFieldBuilder(def);
    }

    public void OnChecking(Func<ExecutionContext, Task> hook) => CheckingHooks.Add(hook);

    public void OnProcessing(Func<ExecutionContext, Task> hook) => ProcessingHook = hook;

    public void SetProcessingHook(Func<ExecutionContext, Task>? hook) => ProcessingHook = hook;

    /// <summary>
    /// Lối tắt cho trường hợp phổ biến nhất: gọi đúng 1 proc, tham số theo đúng thứ tự liệt kê.
    /// Mỗi phần tử là P.Field("...") (lấy từ Filter) hoặc giá trị literal viết thẳng
    /// (P.Value(...) chỉ để dễ đọc, không bắt buộc).
    /// </summary>
    public void Process(string procedureName, params object?[] paramSpecs)
    {
        OnProcessing(async ctx =>
        {
            var values = paramSpecs
                .Select(p => p is FieldRef f
                    ? (ctx.Parameters.TryGetValue(f.Name, out var v) ? v : null)
                    : p)
                .ToArray();

            ctx.Rows = await ctx.CallProcedure(procedureName, values);
        });
    }
}
