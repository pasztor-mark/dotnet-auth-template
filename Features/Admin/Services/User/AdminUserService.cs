using auth_template.Entities;
using auth_template.Entities.Configuration;
using auth_template.Features.Admin.Responses;
using auth_template.Features.Auth.Enums;
using auth_template.Features.Auth.Utilities.Permissions;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Email.Utilities;
using auth_template.Features.Email.Utilities.Client;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Admin.Services.User;

public class AdminUserService(
    IUserUtils _userUtils,
    IPermissionUtility _perms,
    IEmailSenderClient _email,
    AppDbContext _ctx,
    IAuditUtility _audit)
    : IAdminUserService
{
    public async Task<LogicResult<UserManagementResponse>> GetUserForManagementAsync(string userName)
    {
        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(userName);
        if (user is null) return LogicResult<UserManagementResponse>.NotFound("User not found.");

        var perms = await _perms.GetFullPermissionsAsync(user.Id);
        return LogicResult<UserManagementResponse>.Ok(new UserManagementResponse(user, perms));
    }

    public async Task<LogicResult<bool>> BanUserAsync(string userName, string reason)
    {
        var adminId = _userUtils.GetCurrentUserId();
        if (adminId is null) return LogicResult<bool>.Unauthenticated();

        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(userName);
        if (user is null) return LogicResult<bool>.NotFound();

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            user.Ban(reason);
            
            var currentTags = await _ctx.UserTagAssignments.Where(a => a.UserId == user.Id).Select(a => a.TagId).ToListAsync();
            await _perms.UpdateUserTagsAsync(user.Id, [TagConstants.Tags.Banned], currentTags, reason, adminId.Value);

            await _audit.LogUserActivityAsync(user.Id, "User banned", reason, LogType.Authentication);
            
            await _email.SendAsync(MimeMessages.GetAccountBan(user.Email, reason));
            
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return LogicResult<bool>.Error(e.Message);
        }
    }

    public async Task<LogicResult<bool>> UnbanUserAsync(string userName)
    {
        var adminId = _userUtils.GetCurrentUserId();
        if (adminId is null) return LogicResult<bool>.Unauthenticated();

        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(userName);
        if (user is null) return LogicResult<bool>.NotFound();
        if (user.BannedAt is null) return LogicResult<bool>.Conflict("User is not banned.");

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            user.Unban();
            
            await _perms.UpdateUserTagsAsync(user.Id, [TagConstants.Tags.Member], [TagConstants.Tags.Banned], "User unbanned", adminId.Value);

            await _audit.LogUserActivityAsync(user.Id, "User unbanned", "Ban lifted by administrator.", LogType.Authentication);
            
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return LogicResult<bool>.Error(e.Message);
        }
    }


    public async Task<LogicResult<bool>> ConfirmSubscriptionPaymentAsync(string username, Guid targetTierId)
    {
        var adminId = _userUtils.GetCurrentUserId();
        if (adminId is null) return LogicResult<bool>.Unauthorized();

        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(username);
        if (user is null) return LogicResult<bool>.NotFound("Couldn't find this user.");

        string tierName = targetTierId == TagConstants.Tags.PremiumTier ? "Premium" : "Pro";

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            bool tagsUpdated = await _perms.UpdateUserTagsAsync(
                user.Id, 
                [targetTierId], 
                new List<Guid>(), 
                $"Upgraded to {tierName} subscription", 
                adminId.Value
            );

            if (!tagsUpdated)
            {
                await transaction.RollbackAsync();
                return LogicResult<bool>.Error("Failed to update user subscription tier.");
            }

            await _audit.LogUserActivityAsync(user.Id, "Subscription Activated", $"Tier: {tierName}", LogType.General);
            
            await _email.SendAsync(MimeMessages.GetPaymentSuccess(user.Email, tierName));
            
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return LogicResult<bool>.Error(e.Message);
        }
    }
}