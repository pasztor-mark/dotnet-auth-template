using auth_template.Entities.Data;
using auth_template.Features.Auth.Responses.Permissions;

namespace auth_template.Features.Admin.Responses;


public record UserManagementResponse(
    Guid Id,
    string UserName,
    string Email,
    bool IsBanned,
    DateTime? BannedAt,
    string? BanReason,
    List<string> CurrentTags,
    List<string> CurrentFeatures
)
{
    public UserManagementResponse(AppUser user, PermissionTransfer perms) 
        : this(
            user.Id, 
            user.UserName, 
            user.Email, 
            user.BannedAt.HasValue, 
            user.BannedAt, 
            user.BanReason, 
            perms.Tags, 
            perms.Features)
    {
    }
}