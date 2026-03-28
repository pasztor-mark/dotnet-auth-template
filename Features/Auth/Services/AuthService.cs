using System.Security.Claims;
using auth_template.Entities;
using auth_template.Entities.Configuration;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Entities;
using auth_template.Features.Auth.Enums;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Transfer;
using auth_template.Features.Auth.Utilities.Activity;
using auth_template.Features.Auth.Utilities.Permissions;
using auth_template.Features.Auth.Utilities.Tokens.Jwt;
using auth_template.Features.Auth.Utilities.Tokens.Refresh;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Email.Services;
using auth_template.Features.Email.Utilities.Client;
using auth_template.Features.Profile.Entities;
using auth_template.Features.Profile.Utilities;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using auth_template.Utilities.Security.Encryption;
using auth_template.Utilities.Security.Hashing;
using auth_template.Utilities.Security.Pepper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Auth.Services;

public class AuthService(
    IUserUtils _userUtils,
    AppDbContext _ctx,
    UserManager<AppUser> _userManager,
    IJwtGenerator _jwt,
    IRefreshGenerator _refresh,
    SignInManager<AppUser> _signInManager,
    IGeneralHasher _hasher,
    IEncryptor _encryptor,
    ITokenUtility _tokenUtility,
    IPepperProvider _pepperProvider,
    ILogger<AuthService> _logger,
    IPermissionUtility _permissionUtility,
    IActivityBuffer _activity,
    IEmailSenderClient _emailClient,
    IProfileUtility _profileUtility,
    IEmailService _emailService,
    IHttpContextAccessor _http,
    IAuditUtility _auditUtility
) : IAuthService
{
    private readonly int currentVersion = _pepperProvider.GetCurrentVersion();

    private const string _invalidInvite = "This invite has expired. Please try again next time.";

    //POST a signup request
    public async Task<LogicResult<SelfResponse>> RegisterUserAsync(RegisterDto registerDto, string? userAgent,
        string? ipAddress)
    {
        if (userAgent is null) return LogicResult<SelfResponse>.BadRequest("Missing User Agent header.");
        if (ipAddress is null) return LogicResult<SelfResponse>.BadRequest("Invalid address.");
        if (!await _userUtils.CheckEmailAvailabilityAsync(registerDto.email))
            return LogicResult<SelfResponse>.Conflict("E-mail already exists.");

        if (!await _userUtils.CheckUsernameAvailabilityAsync(registerDto.username))
            return LogicResult<SelfResponse>.Conflict("Username already exists.");

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            string normalizedEmail = registerDto.email.ToUpperInvariant().Trim();
            string normalizedUsername = registerDto.username.ToUpperInvariant().Trim();
            var user = new AppUser
            {
                Email = registerDto.email,
                NormalizedEmail = normalizedEmail,
                UserName = registerDto.username.ToLowerInvariant(),
                NormalizedUserName = _userManager.NormalizeName(registerDto.username),
                SecurityStamp = Guid.NewGuid().ToString(),
                PlaintextEmailForIndexing = normalizedEmail,
                PlaintextUsernameForIndexing = normalizedUsername
            };
            var identityResult = await _userManager.CreateAsync(user, registerDto.password);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync();
                string errors = string.Join(", ",
                    identityResult.Errors.Select(e => e.Description));
                _logger.LogError("Issue in persisting new User: {uid} \n {errors}", user.Id, errors);

                return LogicResult<SelfResponse>.Error(errors);
            }
            var createdProfile = new AppUserProfile(user, user.Id);
            await _ctx.UserProfiles.AddAsync(createdProfile);

            var tagAssignmentResult = await _permissionUtility.AssignTagsToUserAsync(
                user.Id,
                [TagConstants.Tags.Member],
                "Registration",
                user.Id
            );
            if (!tagAssignmentResult)
            {
                await transaction.RollbackAsync();
                return LogicResult<SelfResponse>.Error("Failed to assign tags to User. Please try again later");
            }

            var rawRefreshToken = _refresh.GenerateRefreshToken();
            var hashedToken = _hasher.ComputeHmacSha256(rawRefreshToken);
            string encryptedIp = _encryptor.Encrypt(ipAddress);
            string encryptedUa = _encryptor.Encrypt(userAgent);
            string uaHash = _encryptor.GenerateBlindIndex(userAgent, currentVersion);
            string ipHash = _encryptor.GenerateBlindIndex(ipAddress, currentVersion);
            await _ctx.RefreshTokens.AddAsync(new AppRefreshToken(hashedToken, user.Id, encryptedUa,
                encryptedIp, uaHash, ipHash, currentVersion));
            await _auditUtility.LogUserActivityAsync(user.Id, "Registered",
                "This account has been registered on the platform.");

            var createdPreferences = await _userUtils.CreateUserPreferences(user);
            if (!createdPreferences)
            {
                await transaction.RollbackAsync();
                return LogicResult<SelfResponse>.Error("Failed to assign preferences.");
            }

            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            var perms = await _permissionUtility.GetFullPermissionsAsync(user.Id);
            var token = await _jwt.GenerateTokenAsync(user, uaHash, currentVersion);

            _tokenUtility.SetTokenCookies(token, rawRefreshToken);
            _logger.LogInformation("Successful registration for user: {uid}", user.Id);
            return LogicResult<SelfResponse>.Created(new SelfResponse
            {
                UserId = user.Id,
                Features = perms.Features,
                Tags = perms.Tags,
                UserName = user.UserName,
                EmailAddress = user.Email,
            });
        }
        catch (Exception e)
        {
            _ctx.ChangeTracker.Clear();
            await transaction.RollbackAsync();
            _logger.LogError("Exception thrown in registration process: {msg}", e.Message);
            return LogicResult<SelfResponse>.Error("Registration failed due to an internal error.");
        }
    }

    //POST a login request
    public async Task<LogicResult<SelfResponse>> LoginUserAsync(LoginDto dto, string? userAgent, string? ipAddress)
    {
        if (userAgent is null) return LogicResult<SelfResponse>.BadRequest("Missing User Agent header.");
        if (ipAddress is null) return LogicResult<SelfResponse>.BadRequest("Invalid address.");
        if ((dto.email is null && dto.username is null) || dto.password is null)
            return LogicResult<SelfResponse>.BadRequest("Insufficient identifiers provided.");
        bool isWithUsername = dto.email is null && dto.username is not null;
        return await SignInAsync(isWithUsername ? dto.username : dto.email, dto.password, isWithUsername, userAgent,
            ipAddress);
    }
    //POST to log out from one device

    public async Task<LogicResult<bool>> LogoutFromDeviceAsync()
    {
        HttpContext context = _http.HttpContext;
        if (context is null) return LogicResult<bool>.BadRequest("Couldn't get HTTP context. Try again later");
        string? userAgent = context.Request.Headers.UserAgent;
        string? ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (userAgent is null) return LogicResult<bool>.BadRequest("Missing User Agent header.");
        if (ipAddress is null) return LogicResult<bool>.BadRequest("Invalid address.");
        if (string.IsNullOrWhiteSpace(userAgent) || string.IsNullOrWhiteSpace(ipAddress))
            return LogicResult<bool>.BadRequest("Failed to determine which device to sign out from.");

        string? userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return LogicResult<bool>.Unauthenticated("You need to log in first.");


        string lookupAgent = _encryptor.GenerateBlindIndex(userAgent, currentVersion);
        string lookupIp = _encryptor.GenerateBlindIndex(ipAddress, currentVersion);
        try
        {
            var foundResult = await _ctx.RefreshTokens.FirstOrDefaultAsync(t =>
                t.UserAgentIndex.Equals(lookupAgent) && t.IpAddressIndex.Equals(lookupIp) &&
                t.AppUserId.Equals(Guid.Parse(userId)));

            if (foundResult == null && currentVersion > 1)
            {
                int oldVersion = currentVersion - 1;
                string oldLookupAgent = _encryptor.GenerateBlindIndex(userAgent, oldVersion);
                string oldLookupIp = _encryptor.GenerateBlindIndex(ipAddress, oldVersion);

                foundResult = await _ctx.RefreshTokens.FirstOrDefaultAsync(t =>
                    t.UserAgentIndex.Equals(oldLookupAgent) &&
                    t.IpAddressIndex.Equals(oldLookupIp) &&
                    t.AppUserId.Equals(Guid.Parse(userId)) &&
                    t.IndexVersion == oldVersion);
            }

            if (foundResult != null)
            {
                _ctx.RefreshTokens.Remove(foundResult);
                await _ctx.SaveChangesAsync();
            }

            _tokenUtility.ClearTokens(true, true);
            _logger.LogInformation("User {uid} has logged out of {device}", userId, lookupAgent);

            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            await _ctx.DisposeAsync();
            _logger.LogError("Failed to log user out of a device: {e}", e.Message);

            return LogicResult<bool>.NotFound("Couldn't log out");
        }
    }

    //POST to log out from all devices
    public async Task<LogicResult<bool>> LogoutFromAllDevicesAsync()
    {
        string? uidClaim = _http.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uidClaim)) return LogicResult<bool>.Unauthenticated("Session not found");

        Guid uid = Guid.Parse(uidClaim);

        try
        {
            var userTokens = await _ctx.RefreshTokens.Where(t => t.AppUserId == uid).ToListAsync();
        _logger.LogInformation("User logged out of all devices ({uid})", uid);
            if (userTokens.Any())
            {
                _ctx.RemoveRange(userTokens);
            }
            await _ctx.SaveChangesAsync();
        _tokenUtility.ClearTokens(true, true);
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to log user out of all devices: {e}", e.Message);

            throw;
        }


    }

    //POST a token refresh request
    public async Task<LogicResult<bool>> RefreshAsync()
    {
        var refreshToken = _http.HttpContext.Request.Cookies["X-Refresh-Token"];
        var currentUa = _http.HttpContext.Request.Headers.UserAgent.ToString();
        var currentIp = _http.HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _userUtils.RefreshWithTokenAsync(refreshToken, currentUa, currentIp);
        if (result.statusCode == 200)
        {
            _http.HttpContext.Response.Cookies.Append("X-Access-Token", result.data.AccessToken, new CookieOptions
            {
                HttpOnly = true, 
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10) 
            });

            if (!string.IsNullOrWhiteSpace(result.data.RefreshToken))
            {
                _http.HttpContext.Response.Cookies.Append("X-Refresh-Token", result.data.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }
        }
        return LogicResult<bool>.Ok(true);
    }

    //POST a heartbeat
    public void PostHeartbeat(string? userId, ActivityDto dto)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;
        var parsed = Guid.TryParse(userId, out Guid uId);
        if (!parsed || string.IsNullOrWhiteSpace(dto.PageKey) || dto.Seconds <= 0 || dto.Seconds > 6120) return;

        var safePageKey = dto.PageKey.Length > 63
            ? dto.PageKey.Substring(0, 63)
            : dto.PageKey;

        _activity.AddActivity(uId, safePageKey, dto.Seconds);
    }
    //---

    //GET self
    public async Task<LogicResult<SelfResponse>> GetSelfAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var gId))
        {
            return LogicResult<SelfResponse>.Unauthenticated("Please provide a token.");
        }


        var user = await _ctx.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == gId);
        if (user is null) return LogicResult<SelfResponse>.NotFound();
        var permissions = await _permissionUtility.GetFullPermissionsAsync(gId);
        _logger.LogInformation("Self request called by {uid}", userId);

        return LogicResult<SelfResponse>.Ok(new SelfResponse(user, permissions));
    }

    //GET to check email availability
    public async Task<LogicResult<bool>> CheckEmailAvailabilityAsync(string? emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress)) return LogicResult<bool>.BadRequest("Missing E-mail query");
        try
        {
            return LogicResult<bool>.Ok(await _userUtils.CheckEmailAvailabilityAsync(emailAddress));
        }
        catch (Exception _)
        {
            _logger.LogError("Failed to get E-mail availability.");
            return LogicResult<bool>.Error("Something went wrong.");
        }
    }

    //GET to check user availability
    public async Task<LogicResult<bool>> CheckUsernameAvailabilityAsync(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return LogicResult<bool>.BadRequest("Missing username query");
        try
        {
            return LogicResult<bool>.Ok(await _userUtils.CheckUsernameAvailabilityAsync(userName));
        }
        catch (Exception _)
        {
            _logger.LogError("Failed to get username availability.");
            return LogicResult<bool>.Error("Something went wrong.");
        }
    }

    //GET user preferences
    public async Task<LogicResult<PreferenceResponse>> GetPreferencesAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return LogicResult<PreferenceResponse>.BadRequest("Couldn't get your preferences.");
        var prefs = await _ctx.UserPreferences.FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));
        return prefs is null
            ? LogicResult<PreferenceResponse>.NotFound("Couldn't find your preferences.")
            : LogicResult<PreferenceResponse>.Ok(new(prefs));
    }

    //---

    //PATCH to change password
    public async Task<LogicResult<bool>> ChangePasswordAsync(ChangePasswordDto dto, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return LogicResult<bool>.Unauthenticated("You are not authenticated.");
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        var (newPassword, currentPassword) = dto;
        Guid id = Guid.Parse(userId);
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id.Equals(id));
        const string errorMsg = "Invalid credentials";
        if (user is null) return LogicResult<bool>.NotFound(errorMsg);

        bool isPasswordCorrect = await _userManager.CheckPasswordAsync(user, currentPassword);
        if (!isPasswordCorrect) return LogicResult<bool>.NotFound(errorMsg);
        try
        {
            IdentityResult changeRes = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (changeRes.Succeeded)
            {
                await _auditUtility.LogUserActivityAsync(user.Id, "Changed Password",
                    "User has changed their password using reauthentication with their previous password.",
                    LogType.Password);
                await transaction.CommitAsync();
                await this.LogoutFromAllDevicesAsync();
                return LogicResult<bool>.Ok(true);
            }

            await transaction.RollbackAsync();
            return LogicResult<bool>.Error("Update unsuccessful.");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return LogicResult<bool>.Error("Couldn't change your password. Please try again later.");
        }
    }

    //PATCH to change username
    public async Task<LogicResult<bool>> ChangeUsernameAsync(ChangeUsernameDto dto, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return LogicResult<bool>.Unauthenticated("You are not authenticated.");
        string newUsername = dto.newUsername;
        if (string.IsNullOrWhiteSpace(newUsername)) return LogicResult<bool>.BadRequest("Missing new username");
        Guid id = Guid.Parse(userId);
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id.Equals(id));
        if (user is null) return LogicResult<bool>.NotFound("Couldn't find your profile.");
        try
        {
            await _auditUtility.LogUserActivityAsync(id, "Changed Username",
                $"User changed their username");
            var uIndex = _encryptor.GenerateBlindIndex(newUsername.ToUpperInvariant().Trim(), null);
            user.UserName = newUsername.ToLowerInvariant();
            user.NormalizedUserName = newUsername.ToUpperInvariant().Trim();
            user.PlaintextUsernameForIndexing = newUsername.ToUpperInvariant().Trim();
            user.UsernameIndex = uIndex;
            await _ctx.SaveChangesAsync();
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            return LogicResult<bool>.Error("Failed to set new username.");
        }
    }

    //---

    //PUT to recover deactivated account
    public async Task<LogicResult<bool>> RecoverFlaggedUserAsync(ReactivateAccountDto dto)
    {
        string email = dto.Email;
        AppUser? user = await _userUtils.GetAndUpgradeUserByUsernameAsync(email, dto.Username, true, u => u.Flagged);
        if (user is null) return LogicResult<bool>.NotFound("Couldn't find an account to reactivate");
        var profile = await _ctx.UserProfiles.FirstOrDefaultAsync(u => u.UserId == user.Id);
        try
        {
            _logger.LogInformation("User {uid} has reactivated their account.", user.Id);
            await _emailClient.SendSecurityUpdateAsync(email,
                "Account reactivation has been requested for your account.");
            user.Reactivate(dto);
            profile.Reactivate(dto.DisplayName);
            await _emailService.SendForgotPasswordEmailAsync(email, true);
            await _auditUtility.LogUserActivityAsync(user.Id, "User has reactivated their account.",
               type: LogType.Authentication);
            await _ctx.SaveChangesAsync();
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to reactivate account: {e}", e.Message);
            return LogicResult<bool>.Error("Failed to reactivate account. Please try again later");
        }
    }

    //DELETE to flag account for deactivation
    public async Task<LogicResult<bool>> FlagUserForAnonymizationAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return LogicResult<bool>.NotFound();
        var g = Guid.TryParse(userId, out Guid uId);
        if (!g) return LogicResult<bool>.BadRequest();

        AppUser? user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == uId);
        var profile = await _ctx.UserProfiles.FirstOrDefaultAsync(u => u.UserId == uId);
        if (user is null) return LogicResult<bool>.NotFound();
        try
        {
            user.Anonymize();
            profile.Anonymize();
            await this.LogoutFromAllDevicesAsync();
            _logger.LogInformation("User {uid} has deactivated their account.", user.Id);
            await _auditUtility.LogUserActivityAsync(uId, "User has deactivated their account.",
                type: LogType.Authentication);
            
            await _ctx.SaveChangesAsync();
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to deactivate account: {e}", e.Message);
            return LogicResult<bool>.Error("Failed to deactivate account. Please try again later");
        }
    }

    public async Task<LogicResult<List<TagListingResponse>>> SearchTagsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return LogicResult<List<TagListingResponse>>.BadRequest();
        }

        var searchTerm = query.ToUpper();

        var tags = await _ctx.UserTags.AsNoTracking()
            .Where(x => x.Name.ToUpper()
                .Contains(searchTerm))
            .Take(5)
            .ToListAsync();

        var ret = tags.Select(x => new TagListingResponse(x)).ToList();
        return LogicResult<List<TagListingResponse>>.Ok(ret);
    }
    
    public async Task<LogicResult<SelfResponse>> RegisterAdminAsync(RegisterDto registerDto, string? userAgent, string? ipAddress)
{
    if (userAgent is null) return LogicResult<SelfResponse>.BadRequest("Missing User Agent header.");
    if (ipAddress is null) return LogicResult<SelfResponse>.BadRequest("Invalid address.");
    if (!await _userUtils.CheckEmailAvailabilityAsync(registerDto.email))
        return LogicResult<SelfResponse>.Conflict("E-mail already exists.");

    if (!await _userUtils.CheckUsernameAvailabilityAsync(registerDto.username))
        return LogicResult<SelfResponse>.Conflict("Username already exists.");

    await using var transaction = await _ctx.Database.BeginTransactionAsync();
    try
    {
        string normalizedEmail = registerDto.email.ToUpperInvariant().Trim();
        string normalizedUsername = registerDto.username.ToUpperInvariant().Trim();
        var user = new AppUser
        {
            Email = registerDto.email,
            NormalizedEmail = normalizedEmail,
            UserName = registerDto.username.ToLowerInvariant(),
            NormalizedUserName = _userManager.NormalizeName(registerDto.username),
            SecurityStamp = Guid.NewGuid().ToString(),
            PlaintextEmailForIndexing = normalizedEmail,
            PlaintextUsernameForIndexing = normalizedUsername
        };
        
        var identityResult = await _userManager.CreateAsync(user, registerDto.password);
        if (!identityResult.Succeeded)
        {
            await transaction.RollbackAsync();
            string errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            _logger.LogError("Issue in persisting new User: {uid} \n {errors}", user.Id, errors);
            return LogicResult<SelfResponse>.Error(errors);
        }

        var createdProfile = new AppUserProfile(user, user.Id);
        await _ctx.UserProfiles.AddAsync(createdProfile);

        var tagAssignmentResult = await _permissionUtility.AssignTagsToUserAsync(
            user.Id,
            [TagConstants.Tags.Administrator, TagConstants.Tags.Member],
            "Admin Registration",
            user.Id
        );

        if (!tagAssignmentResult)
        {
            await transaction.RollbackAsync();
            return LogicResult<SelfResponse>.Error("Failed to assign tags to User. Please try again later");
        }

        var rawRefreshToken = _refresh.GenerateRefreshToken();
        var hashedToken = _hasher.ComputeHmacSha256(rawRefreshToken);
        string encryptedIp = _encryptor.Encrypt(ipAddress);
        string encryptedUa = _encryptor.Encrypt(userAgent);
        string uaHash = _encryptor.GenerateBlindIndex(userAgent, currentVersion);
        string ipHash = _encryptor.GenerateBlindIndex(ipAddress, currentVersion);
        
        await _ctx.RefreshTokens.AddAsync(new AppRefreshToken(hashedToken, user.Id, encryptedUa, encryptedIp, uaHash, ipHash, currentVersion));
        
        await _auditUtility.LogUserActivityAsync(user.Id, "Registered as Admin", "This account has been registered as an administrator on the platform.");

        var createdPreferences = await _userUtils.CreateUserPreferences(user);
        if (!createdPreferences)
        {
            await transaction.RollbackAsync();
            return LogicResult<SelfResponse>.Error("Failed to assign preferences.");
        }

        await _ctx.SaveChangesAsync();
        await transaction.CommitAsync();
        
        var perms = await _permissionUtility.GetFullPermissionsAsync(user.Id);
        var token = await _jwt.GenerateTokenAsync(user, uaHash, currentVersion);

        _tokenUtility.SetTokenCookies(token, rawRefreshToken);
        _logger.LogInformation("Successful admin registration for user: {uid}", user.Id);
        
        return LogicResult<SelfResponse>.Created(new SelfResponse
        {
            UserId = user.Id,
            Features = perms.Features,
            Tags = perms.Tags,
            UserName = user.UserName,
            EmailAddress = user.Email,
        });
    }
    catch (Exception e)
    {
        _ctx.ChangeTracker.Clear();
        await transaction.RollbackAsync();
        _logger.LogError("Exception thrown in admin registration process: {msg}", e.Message);
        return LogicResult<SelfResponse>.Error("Registration failed due to an internal error.");
    }
}
    private async Task<LogicResult<SelfResponse>> SignInAsync(string identifier, string password, bool isWithUsername,
        string? userAgent, string? ipAddress)
    {
        const string credentialErrorMessage = "Your credentials are invalid. Please try again.";
        AppUser? user = null;
        if (isWithUsername)
        {
            user = await _userUtils.GetAndUpgradeUserByUsernameAsync(identifier, true);
        }
        else
        {
            user = await _userUtils.GetAndUpgradeUserByEmailAsync(identifier, true);
        }


        if (user is null)
            return LogicResult<SelfResponse?>.NotFound(credentialErrorMessage);
        if (user.BannedAt is not null) return LogicResult<SelfResponse>.Unauthorized($"Your account has been banned - {user.BanReason} at {user.BannedAt.ToString()}");
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        var result = await _signInManager.PasswordSignInAsync(user, password, true, true);
        if (result.IsLockedOut)
        {
            await transaction.RollbackAsync();
            return LogicResult<SelfResponse>.Unauthenticated($"Account locked until {user.LockoutEnd}");
        }

        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            return LogicResult<SelfResponse>.Unauthenticated(credentialErrorMessage);
        }

        try
        {

            var existingDevice = await _userUtils.GetAndUpgradeTokenAsync(user.Id.ToString(), userAgent, ipAddress);
            
            string uaIndex = _encryptor.GenerateBlindIndex(userAgent, currentVersion);
            string ipIndex = _encryptor.GenerateBlindIndex(ipAddress, currentVersion);
           
            var rawRefreshToken = _refresh.GenerateRefreshToken();
            var hashedToken = _hasher.ComputeHmacSha256(rawRefreshToken);
            if (existingDevice != null)
            {
                existingDevice.TokenString = hashedToken;
                existingDevice.IssuedAt = DateTime.UtcNow;
                existingDevice.ExpiryUtc = DateTime.UtcNow.AddMonths(AuthConfiguration.RefreshTokenExpirationInMonths);
                _ctx.RefreshTokens.Update(existingDevice);
            }
            else
            {
                await EnforceDeviceLimitAsync(user.Id);

                _ctx.RefreshTokens.Add(new AppRefreshToken(hashedToken, user.Id, _encryptor.Encrypt(userAgent), 
                    _encryptor.Encrypt(ipAddress), uaIndex, ipIndex, currentVersion));
            
            }

            await _auditUtility.LogUserActivityAsync(user.Id,
                $"Login from Device #{await this.GetDeviceNumber(uaIndex, ipIndex, user.Id)}", type: LogType.Login);
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            var permissions = await _permissionUtility.GetFullPermissionsAsync(user.Id);
            var token = await _jwt.GenerateTokenAsync(user, uaIndex, currentVersion, permissions);
            _tokenUtility.SetTokenCookies(token, rawRefreshToken);

            return LogicResult<SelfResponse>.Ok(new SelfResponse
            {
                UserId = user.Id,
                UserName = user.UserName,
                EmailAddress = user.Email,
                Tags = permissions.Tags,
                Features = permissions.Features,
                EmailConfirmed = user.EmailConfirmed,
            });
        }
        catch (Exception)
        {
            _ctx.ChangeTracker.Clear();
            await transaction.RollbackAsync();
            return LogicResult<SelfResponse>.Error("An error occurred while processing your request.");
        }
    }

    private async Task EnforceDeviceLimitAsync(Guid userId)
    {
        var userTokens = await _ctx.RefreshTokens
            .Where(t => t.AppUserId == userId)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        if (userTokens.Count >= 3)
        {
            var tokensToRemove = userTokens.Skip(2).ToList();
            _ctx.RefreshTokens.RemoveRange(tokensToRemove);
        }
    }

   

    private async Task<int> GetDeviceNumber(string uaIndex, string ipIndex, Guid userId)
    {
        var userDevices = await _ctx.RefreshTokens
            .Where(t => t.AppUserId == userId)
            .OrderBy(t => t.IssuedAt) // Vagy egy CreatedAt mező alapján
            .Select(t => new { t.UserAgentIndex, t.IpAddressIndex })
            .ToListAsync();

        int deviceNumber = userDevices.FindIndex(d =>
            d.UserAgentIndex == uaIndex &&
            d.IpAddressIndex == ipIndex) + 1;

        if (deviceNumber <= 0) deviceNumber = userDevices.Count;

        return deviceNumber;
    }
}