using auth_template.Entities.Configuration;
using auth_template.Enums;
using auth_template.Features.Auth.Attributes;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Profile.Responses;
using auth_template.Features.Profile.Services.Profile;
using auth_template.Features.Profile.Transfer.Update;
using auth_template.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace auth_template.Features.Profile.Controllers;

[ApiController]
[EnableRateLimiting(nameof(RateLimits.ProfileUpdate))]
[Route("api/profile")]
public class ProfileController(
    IProfileService _profileService) : ControllerBase
{
    // GET to retrieve current user's full profile (including all nested collections)
    [EnableRateLimiting(nameof(RateLimits.General))]
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ProfileResponse>> GetOwnProfileAsync(CancellationToken ct)
    {
        return ResponseUtility<ProfileResponse>.HttpResponse(await _profileService.GetOwnProfileAsync(ct));
    }

    // GET to retrieve a public profile by username (checks IsPublic visibility)
    [EnableRateLimiting(nameof(RateLimits.General))]
    [HttpGet("u/{id}")]
    public async Task<ActionResult<ProfileResponse>> GetUserProfileAsync([FromRoute] string id, CancellationToken ct)
    {
        return ResponseUtility<ProfileResponse>.HttpResponse(await _profileService.GetUserProfileAsync(id, ct));
    }


    // PUT to update core profile fields (Bio, Headline, Location)

    [HttpPut("u/{userName}/update/core")]
    [OwnerOrPermission(TagConstants.Users.Update)]
    public async Task<ActionResult<ProfileResponse>> UpdateCoreProfileAsync([FromRoute] string userName,
        UpdateCoreProfileDto dto, CancellationToken ct)
    {
        return ResponseUtility<ProfileResponse>.HttpResponse(await _profileService.UpdateCoreAsync(userName, dto, ct));
    }

    // PATCH to update the profile picture (AvatarUrl) only
    [Authorize]
    [HttpPatch("me/update/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfilePictureAsync(
        [FromForm] IFormFile file, CancellationToken ct)
    {
        return ResponseUtility<ProfileResponse>.HttpResponse(await _profileService.UpdateAvatarAsync(file, ct));
    }

    // PATCH to toggle profile visibility (IsPublic)
    [OwnerOrPermission(TagConstants.Users.Update)]
    [HttpPatch("{id}/update/visibility")]
    public async Task<ActionResult<ProfileResponse>> ToggleProfileVisibilityAsync([FromRoute] string id,
        CancellationToken ct)
    {
        return ResponseUtility<ProfileResponse>.HttpResponse(await _profileService.ToggleVisibilityAsync(id, ct));
    }

    // DELETE to wipe all profile data (GDPR/Account deletion)
    [OwnerOrPermission(TagConstants.Users.Delete)]
    [HttpDelete("{id}/delete")]
    public async Task<ActionResult<bool>> DeleteUserProfileAsync([FromRoute] string id, CancellationToken ct)
    {
        return ResponseUtility<bool>.HttpResponse(await _profileService.DeleteProfileAsync(id, ct));
    }

    [OwnerOrPermission(TagConstants.System.AuditLogs)]
    [EnableRateLimiting(nameof(RateLimits.General))]
    [HttpGet("{id}/audit")]
    public async Task<ActionResult<UserUpdateResponse>> GetAuditLogsAsync([FromRoute] string id,
        CancellationToken ct)
    {
        return ResponseUtility<UserUpdateResponse>.HttpResponse(await _profileService.GetAuditLogsAsync(id, ct));
    }
}