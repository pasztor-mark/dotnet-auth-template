using auth_template.Entities.Configuration;
using auth_template.Enums;
using auth_template.Features.Admin.Responses;
using auth_template.Features.Admin.Services.User;
using auth_template.Features.Admin.Transfer;
using auth_template.Features.Auth.Attributes;
using auth_template.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace auth_template.Features.Admin.Controllers;

[Authorize]
[EnableRateLimiting(nameof(RateLimits.General))]
[ApiController]
[Route("api/admin/u/{userName}")]
public class AdminUserController(IAdminUserService _adminUserService) : ControllerBase
{
    // --- User Management ---

    [HttpGet]
    [WithPermissions(TagConstants.Users.Read)]
    public async Task<ActionResult<UserManagementResponse>> GetUserForManagementAsync([FromRoute] string userName)
    {
        return ResponseUtility<UserManagementResponse>.HttpResponse(await _adminUserService.GetUserForManagementAsync(userName));
    }

    [HttpDelete("ban")]
    [WithPermissions(TagConstants.Users.Delete)]
    public async Task<ActionResult<bool>> BanUserAsync([FromRoute] string userName, [FromBody] ReasonDto dto)
    {
        return ResponseUtility<bool>.HttpResponse(await _adminUserService.BanUserAsync(userName, dto.reason));
    }

    [HttpPatch("unban")]
    [WithPermissions(TagConstants.Users.Update)]
    public async Task<ActionResult<bool>> UnbanUserAsync([FromRoute] string userName)
    {
        return ResponseUtility<bool>.HttpResponse(await _adminUserService.UnbanUserAsync(userName));
    }

    // --- Subscription Management ---

    [HttpPatch("subscription/confirm/{targetTierId:guid}")]
    [WithPermissions(TagConstants.Users.Update)]
    public async Task<ActionResult<bool>> ConfirmSubscriptionPaymentAsync([FromRoute] string userName, [FromRoute] Guid targetTierId)
    {
        return ResponseUtility<bool>.HttpResponse(await _adminUserService.ConfirmSubscriptionPaymentAsync(userName, targetTierId));
    }
}