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
    public class UserReportController(IUserReportService _userReportService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(Summary = "Get user management report", Tags = ["UserReport"])]
        public async Task<UserReportResult> GetUsersAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UserReportQueryUpdate update, CancellationToken cancellationToken)
        {
            return await _userReportService.GetUsersAsync(update, cancellationToken);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export user management report as Excel", Tags = ["UserReport"])]
        public async Task<IActionResult> ExportUsersExcelAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UserReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _userReportService.ExportUsersExcelAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export user management report as CSV", Tags = ["UserReport"])]
        public async Task<IActionResult> ExportUsersCsvAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UserReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _userReportService.ExportUsersCsvAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.ReportView, RequireActiveUser = true, RequireAdmin = true)]
        [CustomRateLimit(maxAttemptsCount: 10)]
        [SwaggerOperation(Summary = "Export user management report as PDF", Tags = ["UserReport"])]
        public async Task<IActionResult> ExportUsersPdfAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UserReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var export = await _userReportService.ExportUsersPdfAsync(update, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
