namespace auth_template.Entities.Configuration;

public class PermissionDictionary
{
    private static readonly Guid[] BaseMemberPermissions =
    {
        PermissionIds.Content_Read,
        PermissionIds.Content_Create,
        PermissionIds.Content_Update,
        PermissionIds.Users_Read
    };

    private static readonly Guid[] ProTierPermissions = BaseMemberPermissions.Concat(new[]
    {
        PermissionIds.Content_ProFeature
    }).ToArray();

    private static readonly Guid[] PremiumTierPermissions = ProTierPermissions.Concat(new[]
    {
        PermissionIds.Content_PremiumFeature
    }).ToArray();

    private static readonly Guid[] BaseModeratorPermissions = PremiumTierPermissions.Concat(new[]
    {
        PermissionIds.Content_Delete,
        PermissionIds.Users_Update
    }).ToArray();

    public static readonly Dictionary<Guid, IEnumerable<Guid>> Rules = new()
    {
        [TagConstants.Tags.Banned] = Array.Empty<Guid>(),
        [TagConstants.Tags.Member] = BaseMemberPermissions,
        [TagConstants.Tags.ProTier] = ProTierPermissions,
        [TagConstants.Tags.PremiumTier] = PremiumTierPermissions,
        [TagConstants.Tags.Moderator] = BaseModeratorPermissions,
        [TagConstants.Tags.Administrator] = BaseModeratorPermissions.Concat(new[]
        {
            PermissionIds.Users_Delete,
            PermissionIds.System_AuditLogs,
            PermissionIds.System_ManageRoles,
            PermissionIds.System_ManageSettings
        })
    };
}