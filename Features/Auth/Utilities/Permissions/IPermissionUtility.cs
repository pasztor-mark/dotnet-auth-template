using auth_template.Features.Auth.Responses.Permissions;

namespace auth_template.Features.Auth.Utilities.Permissions;

public interface IPermissionUtility
{
    Task<List<string>> GetUserFeaturesAsync(Guid userId);

    Task<bool> AssignTagsToUserAsync(Guid targetUserId, IEnumerable<Guid> tags, string? reason,
        Guid? assignedBy = null);

    void InvalidateCache(Guid userId);
    Task<PermissionTransfer> GetFullPermissionsAsync(Guid userId);
    Task<List<Guid>> GetUserTagIdsAsync(Guid userId);
    Task<bool> UpdateUserTagsAsync(Guid targetUserId, IEnumerable<Guid> tagsToAdd, IEnumerable<Guid> tagsToRemove, string? reason, Guid? modifiedBy = null);
    Task<List<Guid>> GetUsersOfTagAsync(Guid tagId);
    Guid? GetTagGuidByName(string tagName);
    Task<List<Guid>> GetUsersByTagNameAsync(string tagId);
}