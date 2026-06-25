using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using CoinBank.Services._User.DTOs.Updates;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using System.Security.Claims;
using Utilities.Constants;
using Utilities.Enums;
using Utilities.Exceptions;
using Utilities.Permissions;
using Utilities.Services;
using Utilities.Services.Contracts;
using Utilities.Utilities;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Services._User
{
    public class UserService(
        JwtServiceSettings _jwtSettings,
        IJwtService _jwtService,
        ILogger<UserService> _logger,
        IUserRepository _userRepository,
        IPasswordService _passwordService) : IUserService, IScopedDependency
    {
        public async Task<AccessToken> LoginAsync(AdminLoginUpdate update)
        {
            if (update == null || string.IsNullOrWhiteSpace(update.UserName) || string.IsNullOrWhiteSpace(update.Password))
                throw new AuthorizationException("Invalid username or password");

            var normalizedUserName = NormalizeUserName(update.UserName);

            await EnsureAdminSeededAsync(normalizedUserName);

            var user = await _userRepository.AsQueryable()
                .Where(u => u.UserName == normalizedUserName && u.Role == UserRole.Admin)
                .FirstOrDefaultAsync();

            if (user == null)
                throw new AuthorizationException("Invalid username or password");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                _logger.LogError("Admin user {UserName} has no password hash", normalizedUserName);
                throw new AuthorizationException("Invalid username or password");
            }

            if (!PasswordMatches(update.Password, user.PasswordHash, normalizedUserName))
                throw new AuthorizationException("Invalid username or password");

            await AddLoginDateToUser(user);

            return _jwtService.Generate(GetClaims(user), _jwtSettings.AdminExpiresAfter);
        }

        private async Task EnsureAdminSeededAsync(string normalizedUserName)
        {
            if (string.IsNullOrWhiteSpace(_jwtSettings.AdminUserName) ||
                string.IsNullOrWhiteSpace(_jwtSettings.AdminPassword) ||
                NormalizeUserName(_jwtSettings.AdminUserName) != normalizedUserName)
                return;

            var existing = await _userRepository.AsQueryable()
                .Where(u => u.UserName == normalizedUserName && u.Role == UserRole.Admin)
                .FirstOrDefaultAsync();

            if (existing != null)
                return;

            var user = new User
            {
                UserName = normalizedUserName,
                PasswordHash = _passwordService.Hash(_jwtSettings.AdminPassword),
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                Permissions = [Permissions.ReportView],
                UserPublicKey = Guid.NewGuid().ToString("N"),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                LoginDates = []
            };

            await _userRepository.InsertOneAsync(user);
        }

        private bool PasswordMatches(string password, string passwordHash, string normalizedUserName)
        {
            try
            {
                return _passwordService.Verify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                _logger.LogError(ex, "Invalid BCrypt hash for admin user {UserName}", normalizedUserName);
                return false;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid password hash for admin user {UserName}", normalizedUserName);
                return false;
            }
        }

        private static IEnumerable<Claim> GetClaims(User user)
        {
            var claims = new List<Claim>
            {
                new(Claims.EVMWalletAddress.ToDisplay(), user.EVMWalletAddress ?? "admin"),
                new(Claims.TronWalletAddress.ToDisplay(), user.TronWalletAddress ?? "admin"),
                new(Claims.WalletAddress.ToDisplay(), user.EVMWalletAddress ?? user.TronWalletAddress ?? "admin"),
                new(Claims.PublicKey.ToDisplay(), user.UserPublicKey),
                new(Claims.SecurityStamp.ToDisplay(), user.SecurityStamp),
                new(Claims.UserStatus.ToDisplay(), user.Status.ToString()),
                new(Claims.UserType.ToDisplay(), UserType.Admin.ToDisplay()),
                new(Claims.NetworkType.ToDisplay(), "BEP20"),
            };

            claims.AddRange((user.Permissions ?? []).Select(permission =>
                new Claim(Claims.Permission.ToDisplay(), permission)));

            return claims;
        }

        private async Task<User> AddLoginDateToUser(User user)
        {
            if (user.LoginDates == null || user.LoginDates.Count == 0)
            {
                user.LoginDates = [DateTime.UtcNow];
                await _userRepository.ReplaceOneAsync(user);
                return user;
            }

            user.LoginDates.Add(DateTime.UtcNow);
            user.LoginDates = user.LoginDates
                .OrderByDescending(x => x)
                .Take(20)
                .ToList();

            await _userRepository.ReplaceOneAsync(user);

            return user;
        }

        private static string NormalizeUserName(string userName)
            => userName.Trim().ToLowerInvariant();
    }
}
