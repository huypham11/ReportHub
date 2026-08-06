using Microsoft.EntityFrameworkCore;
using RHApplication.Reports;
using RHInfrastructure.Controllers;
using RHInfrastructure.Persistence;
using RHInfrastructure.Reports;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//DbContext
builder.Services.AddDbContext<SourceDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Source")));

//service
builder.Services.AddScoped<ISalesReportService, SalesReportService>();

//Dynamic Controller (Roslyn) - khai báo report/dir/doc bằng file .cs trong Controllers
// Thư mục này nằm ở gốc solution (ngang hàng RHApi/RHApplication/RHInfrastructure/RHDomain),
// không thuộc project nào - đây là nơi khai báo "dữ liệu", không phải code core.
// Dùng thư mục thật (không phải bin/output) để sửa/lưu file là nhận ngay.
var controllersRuntimeFolder = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "Controllers"));
var controllerLoader = new ControllerLoader(controllersRuntimeFolder);
var loadErrors = controllerLoader.LoadAll();
if (loadErrors.Count > 0)
{
    Console.WriteLine("[ControllerLoader] Có lỗi khi load controller:");
    foreach (var err in loadErrors) Console.WriteLine("  - " + err);
}
builder.Services.AddSingleton(controllerLoader);

builder.Services.AddSingleton(new DapperProcedureExecutor(builder.Configuration.GetConnectionString("Source")!));
builder.Services.AddScoped<ObjectExecutionService>();

var app = builder.Build();

// Hot reload: cấu trúc theo LOẠI (Filter/{code}.cs, Grid/{code}.cs), không theo controller.
// Tên file (không phần mở rộng) chính là code -> khi 1 trong 2 file của cùng code đổi,
// compile lại đúng code đó (ghép Filter + Grid nếu có).
// ControllerLoader.ReloadFromChangedFile tự bắt lỗi và lưu vào LastErrors (tra được qua
// GET /api/controllers/errors) - ở đây chỉ log thêm ra console cho dev đang xem terminal.
void OnFileEvent(string changedFilePath, string eventLabel)
{
    if (!changedFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return;

    var code = Path.GetFileNameWithoutExtension(changedFilePath);
    if (string.IsNullOrEmpty(code)) return;

    Thread.Sleep(200); // tránh đọc file khi trình soạn thảo đang ghi dở
    controllerLoader.ReloadFromChangedFile(changedFilePath);

    if (controllerLoader.LastErrors.TryGetValue(code, out var error))
        Console.WriteLine($"[ControllerLoader] Reload failed ({eventLabel}) for {code}: {error}");
    else
        Console.WriteLine($"[ControllerLoader] Reloaded ({eventLabel}): {code}");
}

var watcher = new FileSystemWatcher(controllersRuntimeFolder, "*.cs")
{
    EnableRaisingEvents = true,
    IncludeSubdirectories = true, // theo dõi cả Filter/ và Grid/ bên trong
    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
};
watcher.Changed += (_, e) => OnFileEvent(e.FullPath, "changed");
watcher.Created += (_, e) => OnFileEvent(e.FullPath, "created");
// Nhiều trình soạn thảo lưu file bằng cách ghi ra file tạm rồi rename đè lên file gốc (atomic save) -
// trường hợp đó Windows chỉ phát sinh sự kiện Renamed, không phát sinh Changed.
watcher.Renamed += (_, e) => OnFileEvent(e.FullPath, "renamed");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
