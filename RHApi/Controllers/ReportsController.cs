using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RHApplication.Reports;

namespace RHApi.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ISalesReportService _salesReportService;
        public ReportsController(ISalesReportService salesReportService)
            => _salesReportService = salesReportService;

        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int top = 10)
        {
            if (fromDate is null || toDate is null)
            {
                return BadRequest("fromDate và toDate là bắt buộc");
            }
            var result = await _salesReportService.GetTopCustomersAsync(fromDate.Value, toDate.Value, top);
            return Ok(result);
        }
    }
}
