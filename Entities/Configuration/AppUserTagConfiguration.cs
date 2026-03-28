using auth_template.Entities.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace auth_template.Entities.Configuration;

public class AppUserTagConfiguration : IEntityTypeConfiguration<AppUserTag>
{
    public void Configure(EntityTypeBuilder<AppUserTag> builder)
    {
        builder.HasData(
            new AppUserTag { Id = TagConstants.Tags.Banned, Name = "Banned" },
            new AppUserTag { Id = TagConstants.Tags.Member, Name = "Member" },
            new AppUserTag { Id = TagConstants.Tags.ProTier, Name = "ProTier" },
            new AppUserTag { Id = TagConstants.Tags.PremiumTier, Name = "PremiumTier" },
            new AppUserTag { Id = TagConstants.Tags.Moderator, Name = "Moderator" },
            new AppUserTag { Id = TagConstants.Tags.Administrator, Name = "Administrator" }
        );
    }
}