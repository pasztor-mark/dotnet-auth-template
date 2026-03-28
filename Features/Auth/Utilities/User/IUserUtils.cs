using System.Linq.Expressions;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Entities;
using auth_template.Features.Auth.Responses;
using auth_template.Utilities;

namespace auth_template.Features.Auth.Utilities.User;

public interface IUserUtils
{
    Guid? GetCurrentUserId();
    Task<AppUser?> GetCurrentUser();
    Task<bool> CheckEmailAvailabilityAsync(string email);
    Task<bool> CheckUsernameAvailabilityAsync(string username);
    Task<bool> CreateUserPreferences(AppUser user);
    Guid? GetUserId();

    Task<AppRefreshToken?> GetAndUpgradeTokenAsync(
        string userId, 
        string userAgent, 
        string ipAddress);

    Task<AppUser?> GetAndUpgradeUserByEmailAsync(string rawEmail, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null);
    Task<AppUser?> GetAndUpgradeUserByUsernameAsync(string rawUsername, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null);
    Task<AppUser?> GetAndUpgradeUserByUsernameAsync(string rawEmail, string rawUsername, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null);
    Task<Guid?> GetAndUpgradeUserIdByUsernameAsync(string rawUsername, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null);
    Task<LogicResult<RefreshResponse>> RefreshWithTokenAsync(string refreshToken, string currentUa, string currentIp);
}