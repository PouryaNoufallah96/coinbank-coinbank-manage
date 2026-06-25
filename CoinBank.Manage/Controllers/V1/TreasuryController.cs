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
    public class TreasuryController(ITreasuryReportService _treasuryReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get treasury program report", Tags = ["Treasury"])]
        public async Task<TreasuryReportResult> GetTreasuryAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TreasuryReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _treasuryReportService.GetTreasuryAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export treasury program report as Excel", Tags = ["Treasury"])]
        public async Task<IActionResult> ExportTreasuryExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TreasuryReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _treasuryReportService.ExportTreasuryExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export treasury program report as CSV", Tags = ["Treasury"])]
        public async Task<IActionResult> ExportTreasuryCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TreasuryReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _treasuryReportService.ExportTreasuryCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export treasury program report as PDF", Tags = ["Treasury"])]
        public async Task<IActionResult> ExportTreasuryPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TreasuryReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _treasuryReportService.ExportTreasuryPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
