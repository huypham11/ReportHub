using Microsoft.AspNetCore.Mvc;
using RHDomain.Controllers;
using RHInfrastructure.Controllers;

namespace RHApi.Controllers
{
    public class ExecuteRequest
    {
        public Dictionary<string, object?> Parameters { get; set; } = new();
    }

    [Route("api/controllers")]
    [ApiController]
    public class DynamicControllersController : ControllerBase
    {
        private readonly ControllerLoader _loader;
        private readonly ObjectExecutionService _executionService;
        private readonly DapperProcedureExecutor _executor;

        public DynamicControllersController(
            ControllerLoader loader,
            ObjectExecutionService executionService,
            DapperProcedureExecutor executor)
        {
            _loader = loader;
            _executionService = executionService;
            _executor = executor;
        }

        // Trả lỗi compile rõ ràng nếu có, thay vì để controller lặng lẽ trả 404/dữ liệu cũ.
        private IActionResult? CheckLoadError(string code)
        {
            if (_loader.LastErrors.TryGetValue(code, out var error))
                return Conflict(new { code, error });
            return null;
        }

        [HttpGet("{code}/filter")]
        public async Task<IActionResult> GetFilter(string code)
        {
            var errorResult = CheckLoadError(code);
            if (errorResult != null) return errorResult;

            var c = _loader.Get(code);
            if (c == null) return NotFound($"Controller '{code}' chưa được load.");

            // SqlExecutor phải được gắn thật ở đây - default value của field có thể
            // cần query SQL Server (VD: lấy kỳ hiện tại, tỷ giá...), không chỉ tính C# thuần.
            var ctx = new RHDomain.Controllers.ExecutionContext
            {
                CurrentUser = new CurrentUserInfo { BranchId = 1 }, // TODO: lấy CurrentUser thật
                SqlExecutor = _executor.ExecuteSqlAsync,
                ScriptExecutor = _executor.ExecuteScriptMultipleAsync
            };

            var fields = new List<object>();
            foreach (var f in c.Fields.Filters)
            {
                var defaultValue = f.DefaultValueFactory != null ? await f.DefaultValueFactory(ctx) : null;
                fields.Add(new
                {
                    f.Field,
                    f.Label,
                    Type = f.Type.ToString(),
                    f.IsRequired,
                    f.LookupSource,
                    DefaultValue = defaultValue
                });
            }

            return Ok(fields);
        }

        [HttpGet("{code}/grid")]
        public IActionResult GetGrid(string code)
        {
            var errorResult = CheckLoadError(code);
            if (errorResult != null) return errorResult;

            var c = _loader.Get(code);
            if (c == null) return NotFound($"Controller '{code}' chưa được load.");

            var columns = c.Fields.Grids.Select(g => new
            {
                g.Field,
                g.Label,
                Type = g.Type.ToString(),
                g.Format,
                g.Width,
                Align = g.Align?.ToString()
            });

            return Ok(columns);
        }

        // Nội dung hiển thị của modal Filter + banner màn Grid - filterTitle khai ở
        // ConfigureFilter, gridTitle/subtitleTemplate khai ở ConfigureGrid, độc lập
        // nhau. null nghĩa là report không khai, FE tự dùng giá trị mặc định (label
        // ở menu / template liệt kê hết field Filter).
        [HttpGet("{code}/banner")]
        public IActionResult GetBanner(string code)
        {
            var errorResult = CheckLoadError(code);
            if (errorResult != null) return errorResult;

            var c = _loader.Get(code);
            if (c == null) return NotFound($"Controller '{code}' chưa được load.");

            return Ok(new
            {
                filterTitle = c.Fields.FilterTitle,
                gridTitle = c.Fields.GridTitle,
                subtitleTemplate = c.Fields.SubtitleTemplate
            });
        }

        [HttpGet("{code}/view")]
        public IActionResult GetView(string code)
        {
            var errorResult = CheckLoadError(code);
            if (errorResult != null) return errorResult;

            var c = _loader.Get(code);
            if (c == null) return NotFound($"Controller '{code}' chưa được load.");

            // Optional - controller không khai báo ConfigureView thì Rows rỗng,
            // FE tự hiểu là "field nào cũng full-width, theo đúng thứ tự ở /filter".
            var rows = c.View.Rows.Select(row => row.Select(item => new
            {
                item.Field,
                item.Span
            }));

            return Ok(rows);
        }

        [HttpPost("{code}/execute")]
        public async Task<IActionResult> Execute(string code, [FromBody] ExecuteRequest request)
        {
            var errorResult = CheckLoadError(code);
            if (errorResult != null) return errorResult;

            // TODO: lấy CurrentUser thật từ auth (JWT/session) khi làm phân quyền - tạm hard-code.
            var currentUser = new CurrentUserInfo { BranchId = 1 };

            var result = await _executionService.ExecuteAsync(code, request.Parameters, currentUser);

            if (!result.Found) return NotFound($"Controller '{code}' chưa được load.");
            if (!result.Success)
                return BadRequest(new { errors = result.Errors.Select(e => new { e.Field, e.Message }) });

            return Ok(new { rows = result.Rows });
        }

        [HttpPost("reload")]
        public IActionResult Reload()
        {
            var errors = _loader.LoadAll();
            return Ok(new { loaded = _loader.All.Select(c => c.Code), errors });
        }

        // Kiểm tra nhanh có report nào đang lỗi compile không, kể cả lỗi phát sinh từ hot
        // reload (sửa file lúc app đang chạy) chứ không chỉ lúc khởi động/gọi /reload.
        [HttpGet("errors")]
        public IActionResult GetErrors()
        {
            return Ok(_loader.LastErrors.Select(kv => new { code = kv.Key, error = kv.Value }));
        }
    }
}
