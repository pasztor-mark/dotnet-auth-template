using auth_template.Entities.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace auth_template.Entities.Configuration;

public class AppUserTagPermissionConfiguration : IEntityTypeConfiguration<AppTagPermission>
{
    public void Configure(EntityTypeBuilder<AppTagPermission> builder)
    {
        var seedData = PermissionDictionary.Rules
            .SelectMany(rule => rule.Value.Select(permissionId => new AppTagPermission
            {
                TagId = rule.Key,        
                PermissionId = permissionId 
            }));

        builder.HasData(seedData);
    }
}