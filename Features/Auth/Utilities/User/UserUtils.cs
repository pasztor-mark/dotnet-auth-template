using System.Linq.Expressions;
using System.Security.Claims;
using auth_template.Entities;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Entities;
using auth_template.Features.Auth.Enums;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Utilities.Tokens.Jwt;
using auth_template.Features.Auth.Utilities.Tokens.Refresh;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using auth_template.Utilities.Security.Encryption;
using auth_template.Utilities.Security.Hashing;
using auth_template.Utilities.Security.Pepper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Auth.Utilities.User;

public class UserUtils(IHttpContextAccessor _http, AppDbContext _ctx, UserManager<AppUser> _userManager, IEncryptor _encryptor, IPepperProvider _pepperProvider, ILogger<UserUtils> _logger, IGeneralHasher _hasher, IJwtGenerator _jwt, IAuditUtility _auditUtility, IRefreshGenerator _refresh) : IUserUtils
{

    private readonly int currentVersion = _pepperProvider.GetCurrentVersion();
    public Guid? GetCurrentUserId()
    {
        return Guid.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
            ? parsed
            : null;
    }

    public async Task<AppUser?> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return null;
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user;
    }
    

    public async Task<bool> CheckEmailAvailabilityAsync(string email)
    {
        //false if email already exists
        // true if email is available
        if (string.IsNullOrWhiteSpace(email)) return false;

        var normalized = _userManager.NormalizeEmail(email);
        int versionToTry = currentVersion;

        while (versionToTry >= 1)
        {
            
            string lookupIndex = _encryptor.GenerateBlindIndex(normalized, versionToTry);
            if (await _ctx.Users.AsNoTracking().IgnoreQueryFilters().AnyAsync(u => u.EmailIndex == lookupIndex))
            {
                return false;
            }
            versionToTry--;
        }
        
        return true;
    }

    public async Task<bool> CheckUsernameAvailabilityAsync(string username)
    {
        //false if username already exists
        // true if username is available
        if (string.IsNullOrWhiteSpace(username)) return false;

        var normalized = _userManager.NormalizeName(username);
        int versionToTry = currentVersion;

        while (versionToTry >= 1)
        {
            string lookupIndex = _encryptor.GenerateBlindIndex(normalized, versionToTry);
            if (await _ctx.Users.AsNoTracking().IgnoreQueryFilters().AnyAsync(u => u.UsernameIndex == lookupIndex))
            {
                return false;
            }
            versionToTry--;
        }

        return true;
    }

    public async Task<bool> CreateUserPreferences(AppUser user)
    {
        try
        {
            if (await _ctx.UserPreferences.AnyAsync(u => u.User.Id == user.Id)) return true;

            await _ctx.UserPreferences.AddAsync(new AppUserPreferences
            {
                User = user,
                UserId = user.Id
            });
            await _ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to create user preferences");
            return false;
        }
    }

    public Guid? GetUserId()
    {
        string? userId = GetUserIdString();
        bool res = Guid.TryParse(userId, out Guid guid);
        
        return res ? guid : null;
    }
    private string? GetUserIdString()
    {
        return _http.HttpContext?.User.FindFirst("sub")?.Value 
               ?? _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    public async Task<AppRefreshToken?> GetAndUpgradeTokenAsync(
        string userId, 
        string userAgent, 
        string ipAddress)
    {
        if (!Guid.TryParse(userId, out var userGuid)) return null;
        
        int versionToTry = currentVersion;

        while (versionToTry >= 1)
        {
            var lookupAgent = _encryptor.GenerateBlindIndex(userAgent, versionToTry);
            var lookupIp = _encryptor.GenerateBlindIndex(ipAddress, versionToTry);

            var token = await _ctx.RefreshTokens
                .FirstOrDefaultAsync(t => 
                    t.AppUserId == userGuid && 
                    t.UserAgentIndex == lookupAgent && 
                    t.IpAddressIndex == lookupIp);

            if (token != null)
            {
                if (versionToTry < currentVersion)
                {
                    token.UserAgentIndex = _encryptor.GenerateBlindIndex(userAgent, currentVersion);
                    token.IpAddressIndex = _encryptor.GenerateBlindIndex(ipAddress, currentVersion);
                    await _ctx.SaveChangesAsync();
                }
                return token;
            }

            versionToTry--;
        }

        return null;
    }
    public async Task<AppUser?> GetAndUpgradeUserByEmailAsync(string rawEmail, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null)
    {
        if (string.IsNullOrWhiteSpace(rawEmail)) return null;

        string normalizedEmail = rawEmail.ToUpperInvariant().Trim();
        int versionToTry = currentVersion;

        while (versionToTry >= 1)
        {
            string lookupIndex = _encryptor.GenerateBlindIndex(normalizedEmail, versionToTry);

            var query = _ctx.Users.Where(u => u.EmailIndex == lookupIndex);
            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            AppUser? user = await query.FirstOrDefaultAsync();

            if (user is not null)
            {
                if (versionToTry < currentVersion && !user.Flagged)
                {
                    user.EmailIndex = _encryptor.GenerateBlindIndex(normalizedEmail, currentVersion);
                    user.Email = _encryptor.Encrypt(rawEmail);
                    user.NormalizedEmail = _encryptor.Encrypt(normalizedEmail);
                }
                return user;
            }

            versionToTry--;
        }

        return null;
    }
    public async Task<LogicResult<RefreshResponse>> RefreshWithTokenAsync(string refreshToken, string currentUa, string currentIp)
{
    await using var transaction = await _ctx.Database.BeginTransactionAsync();
    try
    {
        if (string.IsNullOrEmpty(refreshToken))
            return LogicResult<RefreshResponse>.Unauthenticated("Refresh token is required.");
            
        if (string.IsNullOrEmpty(currentUa) || string.IsNullOrEmpty(currentIp))
            return LogicResult<RefreshResponse>.BadRequest("Missing client identification headers.");

        var uaHash = _encryptor.GenerateBlindIndex(currentUa, currentVersion);
        var ipHash = _encryptor.GenerateBlindIndex(currentIp, currentVersion);
        var hashedInput = _hasher.ComputeHmacSha256(refreshToken);
        
        var result = await _ctx.RefreshTokens
            .Include(t => t.AppUser)
            .FirstOrDefaultAsync(t => t.TokenString == hashedInput && t.UserAgentIndex.Equals(uaHash));
            
        if (result is null)
        {
            int oldVersion = currentVersion - 1;
            if (oldVersion > 0)
            {
                var oldHash = _encryptor.GenerateBlindIndex(currentUa, oldVersion);
                result = await _ctx.RefreshTokens.Include(t => t.AppUser).FirstOrDefaultAsync(t =>
                    t.TokenString == hashedInput && t.UserAgentIndex == oldHash && t.IndexVersion == oldVersion);
                    
                if (result != null)
                {
                    result.UserAgentIndex = uaHash;
                    result.IndexVersion = currentVersion;
                    _ctx.RefreshTokens.Update(result);
                    await _ctx.SaveChangesAsync();
                    _logger.LogInformation("Security Migration: Migrated RefreshToken for User {Id} to Pepper v{V}", result.AppUserId, currentVersion);
                }
            }
        }

        if (result?.AppUser is null)
            return LogicResult<RefreshResponse>.Unauthenticated("User not found or invalid token.");
            
        if (result.UserAgentIndex != uaHash)
        {
            _ctx.RefreshTokens.Remove(result);
            await _ctx.SaveChangesAsync();
            _logger.LogWarning("Security Alert: Device mismatch detected for {uid}", result.AppUserId);
            return LogicResult<RefreshResponse>.Unauthorized("Device mismatch detected. Please log in again.");
        }

        if (DateTime.UtcNow > result.ExpiryUtc)
            return LogicResult<RefreshResponse>.Unauthorized("Refresh token has expired. Please log in again.");

        string? newRawToken = null;

        if (result.ExpiryUtc <= DateTime.UtcNow.AddDays(AuthConfiguration.RefreshTokenReplacementGracePeriodInDays))
        {
            newRawToken = await ReplaceRefreshTokenAsync(result, uaHash, ipHash, currentVersion);

            if (newRawToken is null)
            {
                await transaction.RollbackAsync();
                return LogicResult<RefreshResponse>.Unauthenticated("Failed to issue new refresh token.");
            }
        }

        await _ctx.SaveChangesAsync();
        await transaction.CommitAsync();

        string jwtToken = await _jwt.GenerateTokenAsync(result.AppUser, uaHash, currentVersion);
        
        return LogicResult<RefreshResponse>.Ok(new RefreshResponse
        {
            AccessToken = jwtToken,
            RefreshToken = newRawToken
        });
    }
    catch (Exception _)
    {
        await transaction.RollbackAsync();
        return LogicResult<RefreshResponse>.Error("Something went wrong during token refresh.");
    }
}
    public async Task<AppUser?> GetAndUpgradeUserByUsernameAsync(string rawUsername, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null)
    {
        if (string.IsNullOrWhiteSpace(rawUsername)) return null;
        string normalizedUsername = rawUsername.Trim().ToUpperInvariant();
    
        int versionToTry = currentVersion;

        while (versionToTry >= 1)
        {
            string lookupIndex = _encryptor.GenerateBlindIndex(normalizedUsername, versionToTry);

            var query = _ctx.Users.Where(u => u.UsernameIndex == lookupIndex);
        
            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }
        
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            AppUser? user = await query.FirstOrDefaultAsync();

            if (user is not null)
            {
                if (versionToTry < currentVersion && !user.Flagged)
                {
                    user.UsernameIndex = _encryptor.GenerateBlindIndex(normalizedUsername, currentVersion);
                    user.NormalizedUserName = normalizedUsername; 
                }
                return user;
            }

            versionToTry--;
        }

        return null;
    }
    public async Task<Guid?> GetAndUpgradeUserIdByUsernameAsync(string rawUsername, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null)
    {
        if (string.IsNullOrWhiteSpace(rawUsername)) return null;
        string normalizedUsername = rawUsername.Trim().ToUpperInvariant();
    
        int versionToTry = currentVersion;

        while (versionToTry >= 1)
        {
            string lookupIndex = _encryptor.GenerateBlindIndex(normalizedUsername, versionToTry);

            var query = _ctx.Users.AsNoTracking().Where(u => u.UsernameIndex == lookupIndex);
        
            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }
        
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            AppUser? user = await query.FirstOrDefaultAsync();

            if (user is not null)
            {
                if (versionToTry < currentVersion && !user.Flagged)
                {
                    user.UsernameIndex = _encryptor.GenerateBlindIndex(normalizedUsername, currentVersion);
                    user.NormalizedUserName = normalizedUsername; 
                }
                return user.Id;
            }

            versionToTry--;
        }

        return null;
    }
    public async Task<AppUser?> GetAndUpgradeUserByUsernameAsync(string rawEmail, string rawUsername, bool ignoreQueryFilters = false, Expression<Func<AppUser, bool>>? predicate = null)
{
    if (string.IsNullOrWhiteSpace(rawEmail) || string.IsNullOrWhiteSpace(rawUsername)) return null;

    string normalizedEmail = rawEmail.Trim().ToUpperInvariant();
    string normalizedUsername = rawUsername.Trim().ToUpperInvariant();
    
    int versionToTry = currentVersion;

    while (versionToTry >= 1)
    {
        string emailLookupIndex = _encryptor.GenerateBlindIndex(normalizedEmail, versionToTry);
        string usernameLookupIndex = _encryptor.GenerateBlindIndex(normalizedUsername, versionToTry);

        var query = _ctx.Users.Where(u => u.EmailIndex == emailLookupIndex && u.UsernameIndex == usernameLookupIndex);

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        AppUser? user = await query.FirstOrDefaultAsync();

        if (user is not null)
        {
            if (versionToTry < currentVersion && !user.Flagged)
            {
                user.EmailIndex = _encryptor.GenerateBlindIndex(normalizedEmail, currentVersion);
                user.Email = _encryptor.Encrypt(rawEmail.Trim());
                user.NormalizedEmail = _encryptor.Encrypt(normalizedEmail);

                user.UsernameIndex = _encryptor.GenerateBlindIndex(normalizedUsername, currentVersion);
                user.NormalizedUserName = normalizedUsername;
            }
            return user;
        }

        versionToTry--;
    }

    return null;
}
    private async Task<string> ReplaceRefreshTokenAsync(
        AppRefreshToken oldToken,
        string newUaHash,
        string newIpHash,
        int newVersion)
    {
        _ctx.RefreshTokens.Remove(oldToken);

        string rawToken = _refresh.GenerateRefreshToken();
        string hashedToken = _hasher.ComputeHmacSha256(rawToken);

        var newToken = new AppRefreshToken(
            hashedToken,
            oldToken.AppUserId,
            oldToken.UserAgent,
            oldToken.IpAddress,
            newUaHash,
            newIpHash,
            newVersion
        );

        await _ctx.RefreshTokens.AddAsync(newToken);
        await _auditUtility.LogUserActivityAsync(oldToken.AppUserId, "Refresh Token reassigned",
            "User's refresh token has been automatically reassigned due to expiration.", LogType.Authentication);
        return rawToken;
    }
}