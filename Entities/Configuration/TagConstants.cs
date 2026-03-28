namespace auth_template.Entities.Configuration;

public static class TagConstants
{
    public static class Tags
    {
        public static readonly Guid Banned = Guid.Parse("bad00000-0000-0000-0000-000000000000");
        public static readonly Guid Member = Guid.Parse("0d03ccf4-4a86-4e89-a4d5-53f641c391db");
        public static readonly Guid ProTier = Guid.Parse("da7a0000-0000-0000-0000-000000000001");
        public static readonly Guid PremiumTier = Guid.Parse("da7a0000-0000-0000-0000-000000000002");
        public static readonly Guid Moderator = Guid.Parse("73fdf799-125e-4b4d-bff7-d11c4d8f436e");
        public static readonly Guid Administrator = Guid.Parse("6adcdd65-ab35-4a76-a8a5-ce1f0168b729");
    }

    public static class Content
    {
        public const string Read = "Content.Read";
        public const string Create = "Content.Create";
        public const string Update = "Content.Update";
        public const string Delete = "Content.Delete";
        public const string ProFeature = "Content.ProFeature.Access";
        public const string PremiumFeature = "Content.PremiumFeature.Access";
    }

    public static class Users
    {
        public const string Read = "Users.Read";
        public const string Update = "Users.Update";
        public const string Delete = "Users.Delete";
    }

    public static class System
    {
        public const string AuditLogs = "System.AuditLogs.View";
        public const string ManageRoles = "System.Roles.Manage";
        public const string ManageSettings = "System.Settings.Manage";
    }
}