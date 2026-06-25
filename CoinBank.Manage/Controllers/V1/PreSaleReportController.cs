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
    public class PreSaleReportController(IPreSaleReportService _preSaleReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get pre-sale report", Tags = ["PreSaleReport"])]
        public async Task<PreSaleReportResult> GetPreSalesAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PreSaleReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _preSaleReportService.GetPreSalesAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export pre-sale report as Excel", Tags = ["PreSaleReport"])]
        public async Task<IActionResult> ExportPreSalesExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PreSaleReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _preSaleReportService.ExportPreSalesExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export pre-sale report as CSV", Tags = ["PreSaleReport"])]
        public async Task<IActionResult> ExportPreSalesCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PreSaleReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _preSaleReportService.ExportPreSalesCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export pre-sale report as PDF", Tags = ["PreSaleReport"])]
        public async Task<IActionResult> ExportPreSalesPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PreSaleReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _preSaleReportService.ExportPreSalesPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
