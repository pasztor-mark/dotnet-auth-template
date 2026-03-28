namespace auth_template.Entities.Configuration;

public static class PermissionIds
{
    public static readonly Guid Content_Read = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Content_Create = Guid.Parse("11111111-1111-1111-1111-111111111112");
    public static readonly Guid Content_Update = Guid.Parse("11111111-1111-1111-1111-111111111113");
    public static readonly Guid Content_Delete = Guid.Parse("11111111-1111-1111-1111-111111111114");
    public static readonly Guid Content_ProFeature = Guid.Parse("11111111-1111-1111-1111-111111111115");
    public static readonly Guid Content_PremiumFeature = Guid.Parse("11111111-1111-1111-1111-111111111116");

    public static readonly Guid Users_Read = Guid.Parse("22222222-2222-2222-2222-222222222221");
    public static readonly Guid Users_Update = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Users_Delete = Guid.Parse("22222222-2222-2222-2222-222222222223");

    public static readonly Guid System_AuditLogs = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid System_ManageRoles = Guid.Parse("33333333-3333-3333-3333-333333333332");
    public static readonly Guid System_ManageSettings = Guid.Parse("33333333-3333-3333-3333-333333333333");
}