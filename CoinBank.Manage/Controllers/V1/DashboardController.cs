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
    public class DashboardController(IDashboardReportService _dashboardReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get dashboard report", Tags = ["Dashboard"])]
        public async Task<DashboardReportResult> GetDashboardAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _dashboardReportService.GetDashboardAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export dashboard report as Excel", Tags = ["Dashboard"])]
        public async Task<IActionResult> ExportDashboardExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReportExportUpdate update, CancellationToken cancellationToken)
        {
            var export = await _dashboardReportService.ExportDashboardExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export dashboard report as CSV", Tags = ["Dashboard"])]
        public async Task<IActionResult> ExportDashboardCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReportExportUpdate update, CancellationToken cancellationToken)
        {
            var export = await _dashboardReportService.ExportDashboardCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export dashboard report as PDF", Tags = ["Dashboard"])]
        public async Task<IActionResult> ExportDashboardPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReportExportUpdate update, CancellationToken cancellationToken)
        {
            var export = await _dashboardReportService.ExportDashboardPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
