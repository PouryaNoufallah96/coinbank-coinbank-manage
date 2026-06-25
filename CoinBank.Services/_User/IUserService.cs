using CoinBank.Services._User.DTOs.Updates;
using Utilities.Services;

namespace CoinBank.Services._User
{
    public interface IUserService
    {
        Task<AccessToken> LoginAsync(AdminLoginUpdate update);
    }
}
