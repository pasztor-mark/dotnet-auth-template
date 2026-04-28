using auth_template.Entities;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Profile.Responses;
using auth_template.Features.Profile.Transfer.Update;
using auth_template.Features.Profile.Utilities;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Profile.Services.Profile;

public class ProfileService(
    IProfileUtility _profileUtility,
    IUserUtils _userUtils,
    AppDbContext _ctx,
    ILogger<ProfileService> _logger,
    IAuditUtility _audit) : IProfileService
{
    private readonly string _notFoundMessage = "Profile not found.";

    // GET to retrieve current user's full profile (including all nested collections)
    public async Task<LogicResult<ProfileResponse>> GetOwnProfileAsync(CancellationToken ct)
    {
        Guid? id = _userUtils.GetCurrentUserId();
        if (id is null) return LogicResult<ProfileResponse>.Unauthenticated();
        var prof = await _profileUtility.GetProfileByUserIdAsync(id.Value);
        if (prof is null) return LogicResult<ProfileResponse>.NotFound();
        return LogicResult<ProfileResponse>.Ok(new(prof));
    }

    // GET to retrieve a public profile by username (checks IsPublic visibility)
    public async Task<LogicResult<ProfileResponse>> GetUserProfileAsync(string identifier, CancellationToken ct)
    {
        var prof = await _profileUtility.GetProfileByUserNameAsync(identifier);
        if (prof is null) return LogicResult<ProfileResponse>.NotFound();
        return LogicResult<ProfileResponse>.Ok(new(prof));
    }

    // PUT to update core profile fields (Bio, Headline, Location)
    public async Task<LogicResult<ProfileResponse>> UpdateCoreAsync(string username, UpdateCoreProfileDto dto,
        CancellationToken ct)
    {
        var userId = await _userUtils.GetAndUpgradeUserIdByUsernameAsync(username);
        if (userId is null) return LogicResult<ProfileResponse>.NotFound(_notFoundMessage);

        try
        {
            var profile = await _ctx.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);

            if (profile is null) return LogicResult<ProfileResponse>.NotFound(_notFoundMessage);

            var (displayName, bio, headline, location, dateOfBirth, isPublic) = dto;

            bool hasChanges = false;

            if (bio is not null && profile.Bio != bio)
            {
                profile.Bio = bio.Trim();
                hasChanges = true;
            }

            if (headline is not null && profile.Headline != headline)
            {
                profile.Headline = headline.Trim();
                hasChanges = true;
            }

            if (location is not null && profile.Location != location)
            {
                profile.Location = location.Trim();
                hasChanges = true;
            }

            if (dateOfBirth.HasValue && profile.DateOfBirth != dateOfBirth.Value)
            {
                profile.DateOfBirth = dateOfBirth.Value;
                hasChanges = true;
            }

            if (isPublic.HasValue && profile.IsPublic != isPublic.Value)
            {
                profile.IsPublic = isPublic.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _ctx.SaveChangesAsync(ct);

                _logger.LogInformation("Core profile updated for user {UserId}", userId);
                await _audit.LogUserActivityAsync(userId.Value, "Updated core profile details");
            }

            return LogicResult<ProfileResponse>.Ok(new(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating core profile for {UserId}", userId);
            return LogicResult<ProfileResponse>.Error("An error occurred while updating the profile.");
        }
    }

    // PATCH to update the profile picture (AvatarUrl) only
    public async Task<LogicResult<ProfileResponse>> UpdateAvatarAsync(IFormFile avatarFile, CancellationToken ct)
    {
        if (avatarFile.Length > 10 * 1024 * 1024) return LogicResult<ProfileResponse>.BadRequest("File size must be under 10mb.");
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return LogicResult<ProfileResponse>.BadRequest("Invalid image format.");
        }
        
        

        var userId = _userUtils.GetCurrentUserId();
        if (userId is null) return LogicResult<ProfileResponse>.Unauthenticated();

        var fileName = $"{userId}-{Guid.NewGuid()}{extension}";

        //var avatarUrl = await _blobUtility.UploadImageAsync(avatarFile, fileName, "avatars");
        var profile = await _ctx.UserProfiles.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);
        if (profile == null)
        {
            return LogicResult<ProfileResponse>.NotFound("Profile not found.");
        }

        var oldAvatarUrl = profile.User.AvatarUrl;


        //profile.AvatarUrl = avatarUrl;
        await _ctx.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldAvatarUrl) && !oldAvatarUrl.Contains("default-avatar"))
        {
            //await _blobUtility.DeleteImageAsync(oldAvatarUrl, "avatars");
        }
        await _ctx.SaveChangesAsync(ct);

        var response = new ProfileResponse(profile);

        return LogicResult<ProfileResponse>.Ok(response);
    }

    // PATCH to toggle profile visibility (IsPublic)
    public async Task<LogicResult<ProfileResponse>> ToggleVisibilityAsync(string username, CancellationToken ct)
    {
        var prof = await _profileUtility.GetProfileByUserNameAsync(username, true, true);

        if (prof is null) return LogicResult<ProfileResponse>.NotFound(_notFoundMessage);
        try
        {
            prof.IsPublic = !prof.IsPublic;
            await _ctx.SaveChangesAsync(ct);
            return LogicResult<ProfileResponse>.Ok(new(prof));
        }
        catch (Exception e)
        {
            return LogicResult<ProfileResponse>.Error("Something went wrong.");
        }
    }

    // DELETE to wipe all profile data (GDPR/Account deletion)
    public async Task<LogicResult<bool>> DeleteProfileAsync(string username, CancellationToken ct)
    {
        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(username);
        if (user is null) return LogicResult<bool>.NotFound();
        var prof = await _ctx.UserProfiles.AsSplitQuery().FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken: ct);

        if (prof is null) return LogicResult<bool>.NotFound(_notFoundMessage);
        try
        {
            prof.Anonymize();
            await _ctx.SaveChangesAsync(ct);
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            return LogicResult<bool>.Error("Something went wrong");
        }
    }

    public async Task<LogicResult<UserUpdateResponse>> GetAuditLogsAsync(string id, CancellationToken ct)
    {
        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(id);
        if (user is null) return LogicResult<UserUpdateResponse>.NotFound();
        try
        {
            var updates = await _ctx.UserUpdates.AsNoTracking().IgnoreQueryFilters().Where(x => x.UserId == user.Id)
                .ToListAsync(ct);
            return LogicResult<UserUpdateResponse>.Ok(new UserUpdateResponse(user, updates));
        }
        catch (Exception e)
        {
            return LogicResult<UserUpdateResponse>.Error(e.Message);
        }
    }
}