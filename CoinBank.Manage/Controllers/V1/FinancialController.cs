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
    public class FinancialController(IFinancialReportService _financialReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get financial accounting report", Tags = ["Financial"])]
        public async Task<FinancialReportResult> GetFinancialsAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FinancialReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _financialReportService.GetFinancialsAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export financial accounting report as Excel", Tags = ["Financial"])]
        public async Task<IActionResult> ExportFinancialsExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FinancialReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _financialReportService.ExportFinancialsExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export financial accounting report as CSV", Tags = ["Financial"])]
        public async Task<IActionResult> ExportFinancialsCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FinancialReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _financialReportService.ExportFinancialsCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export financial accounting report as PDF", Tags = ["Financial"])]
        public async Task<IActionResult> ExportFinancialsPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FinancialReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _financialReportService.ExportFinancialsPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
