using auth_template.Entities.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace auth_template.Entities.Configuration;

public class AppPermissionConfiguration : IEntityTypeConfiguration<AppPermission>
{
    public void Configure(EntityTypeBuilder<AppPermission> builder)
    {
        builder.HasData(
            new AppPermission { Id = PermissionIds.Content_Read, Name = TagConstants.Content.Read, Description = "Read generic content" },
            new AppPermission { Id = PermissionIds.Content_Create, Name = TagConstants.Content.Create, Description = "Create generic content" },
            new AppPermission { Id = PermissionIds.Content_Update, Name = TagConstants.Content.Update, Description = "Update generic content" },
            new AppPermission { Id = PermissionIds.Content_Delete, Name = TagConstants.Content.Delete, Description = "Delete generic content" },
            new AppPermission { Id = PermissionIds.Content_ProFeature, Name = TagConstants.Content.ProFeature, Description = "Access pro tier features" },
            new AppPermission { Id = PermissionIds.Content_PremiumFeature, Name = TagConstants.Content.PremiumFeature, Description = "Access premium tier features" },

            new AppPermission { Id = PermissionIds.Users_Read, Name = TagConstants.Users.Read, Description = "Read basic user profiles" },
            new AppPermission { Id = PermissionIds.Users_Update, Name = TagConstants.Users.Update, Description = "Update other users" },
            new AppPermission { Id = PermissionIds.Users_Delete, Name = TagConstants.Users.Delete, Description = "Delete users" },

            new AppPermission { Id = PermissionIds.System_AuditLogs, Name = TagConstants.System.AuditLogs, Description = "View system audit logs" },
            new AppPermission { Id = PermissionIds.System_ManageRoles, Name = TagConstants.System.ManageRoles, Description = "Manage user roles and tags" },
            new AppPermission { Id = PermissionIds.System_ManageSettings, Name = TagConstants.System.ManageSettings, Description = "Manage global system settings" }
        );
    }
}