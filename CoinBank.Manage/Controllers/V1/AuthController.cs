using Asp.Versioning;
using CoinBank.Services._User;
using CoinBank.Services._User.DTOs.Updates;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utilities.Api;
using Utilities.Attributes;
using Utilities.Filters;
using Utilities.Services;

namespace CoinBank.Manage.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController(IUserService _userService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [CustomRateLimit(maxAttemptsCount: 10, requestBodyKey: nameof(AdminLoginUpdate.UserName))]
        [SwaggerOperation(Summary = "For admin login", Tags = ["Auth"])]
        public async Task<AccessToken> Login(AdminLoginUpdate update)
        {
            return await _userService.LoginAsync(update);
        }
    }
}
