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
    public class TokenReleaseController(ITokenReleaseReportService _tokenReleaseReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get token release report", Tags = ["TokenRelease"])]
        public async Task<TokenReleaseReportResult> GetTokenReleasesAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _tokenReleaseReportService.GetTokenReleasesAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export token release report as Excel", Tags = ["TokenRelease"])]
        public async Task<IActionResult> ExportTokenReleasesExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _tokenReleaseReportService.ExportTokenReleasesExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export token release report as CSV", Tags = ["TokenRelease"])]
        public async Task<IActionResult> ExportTokenReleasesCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _tokenReleaseReportService.ExportTokenReleasesCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export token release report as PDF", Tags = ["TokenRelease"])]
        public async Task<IActionResult> ExportTokenReleasesPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _tokenReleaseReportService.ExportTokenReleasesPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
