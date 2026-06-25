using Asp.Versioning;
using CoinBank.Services._Report;
using CoinBank.Services._Report.DTOs.Results;
using CoinBank.Services._Report.DTOs.Updates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;
using Utilities.Api;
using Utilities.Attributes;
using Utilities.Filters;
using Utilities.Permissions;

namespace CoinBank.Manage.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SwapReportController(ISwapReportService _swapReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get swap report", Tags = ["SwapReport"])]
        public async Task<SwapReportResult> GetSwapsAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SwapReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _swapReportService.GetSwapsAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export swap report as Excel", Tags = ["SwapReport"])]
        public async Task<IActionResult> ExportSwapsExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SwapReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _swapReportService.ExportSwapsExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export swap report as CSV", Tags = ["SwapReport"])]
        public async Task<IActionResult> ExportSwapsCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SwapReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _swapReportService.ExportSwapsCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export swap report as PDF", Tags = ["SwapReport"])]
        public async Task<IActionResult> ExportSwapsPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SwapReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _swapReportService.ExportSwapsPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
