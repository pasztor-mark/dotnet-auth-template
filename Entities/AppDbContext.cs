using auth_template.Entities.Configuration;
using auth_template.Entities.Data;
using auth_template.Entities.Interfaces;
using auth_template.Features.Auth.Entities;
using auth_template.Features.Profile.Entities;
using auth_template.Utilities.Security.Encryption;
using auth_template.Utilities.Security.Pepper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace auth_template.Entities;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IEncryptor _encryptor,
    IPepperProvider _pepperProvider)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    private readonly int _currentVersion = _pepperProvider.GetCurrentVersion();

    //Auth
    public DbSet<AppUser> Users { get; set; }
    public DbSet<AppUserUpdates> UserUpdates { get; set; }
    public DbSet<AppUserPreferences> UserPreferences { get; set; }
    public DbSet<AppRefreshToken> RefreshTokens { get; set; }
    public DbSet<AppPermission> Permissions { get; set; }
    public DbSet<AppTagPermission> TagPermissions { get; set; }
    public DbSet<AppUserTag> UserTags { get; set; }
    public DbSet<AppUserTagAssignment> UserTagAssignments { get; set; }
    public DbSet<AppUserTagPermissions> UserTagPermissions { get; set; }
    public DbSet<AppUserActivity> UserActivities { get; set; }

    //Profile
    public DbSet<AppUserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
            }
        }

        ValueConverter<string, string> converter = new(v => _encryptor.Encrypt(v), v => _encryptor.Decrypt(v));
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.Email).HasConversion(converter);
            entity.Property(u => u.NormalizedEmail).HasConversion(converter);
            entity.Property(u => u.UserName).HasConversion(converter);
            entity.Property(u => u.NormalizedUserName).HasConversion(converter);

            entity.Property(u => u.EmailIndex).IsRequired();
            entity.HasIndex(u => u.EmailIndex).IsUnique();

            entity.Property(u => u.UsernameIndex).IsRequired();
            entity.HasIndex(u => u.UsernameIndex).IsUnique();
            entity.ToTable("Users");
            entity.HasOne<AppUserUpdates>();
            entity.HasOne<AppUserProfile>(u => u.Profile).WithOne(p => p.User).HasForeignKey<AppUserProfile>(p => p.UserId);
            entity.HasQueryFilter(u => !u.Flagged && u.BannedAt == null);
        });

        builder.Entity<AppUserActivity>()
            .HasKey(ua => new { ua.UserId, ua.Date, ua.PageKey });

        builder.Entity<AppUserActivity>()
            .Property(ua => ua.Date)
            .HasColumnType("date");
        builder.Entity<AppUserActivity>()
            .Property(b => b.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
        builder.Entity<AppUserUpdates>()
            .Property(b => b.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
        builder.Entity<AppTagPermission>().HasKey(tp => new { tp.TagId, tp.PermissionId });
        builder.Entity<AppTagPermission>().HasIndex(tp => tp.PermissionId);

        builder.Entity<AppUserTagPermissions>().HasKey(utp => new { utp.UserId, utp.TagId });
        builder.Entity<AppUserTagPermissions>().HasIndex(utp => utp.TagId);

        builder.Entity<AppPermission>().HasIndex(p => p.Name).IsUnique();

        builder.Entity<AppUserTagAssignment>().HasIndex(a => a.UserId);
        builder.Entity<AppUserTag>().HasKey(t => t.Id);


        //Profile
        builder.Entity<AppUserProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);

            entity.HasIndex(p => p.Location);

            entity.Property(p => p.Bio)
                .HasConversion(
                    v => v != null ? _encryptor.Encrypt(v) : null,
                    v => v != null ? _encryptor.Decrypt(v) : null
                );

            entity.Property(p => p.Headline)
                .HasConversion(
                    v => v != null ? _encryptor.Encrypt(v) : null,
                    v => v != null ? _encryptor.Decrypt(v) : null
                );

            entity.Property(p => p.DateOfBirth)
                .HasConversion(
                    v => v.HasValue ? _encryptor.Encrypt(v.Value.ToString("yyyy-MM-dd")) : null,
                    v => v != null ? DateTime.Parse(_encryptor.Decrypt(v)) : (DateTime?)null
                );
        });

        builder.ApplyConfiguration(new AppUserTagConfiguration());
        builder.ApplyConfiguration(new AppPermissionConfiguration());
        builder.ApplyConfiguration(new AppUserTagPermissionConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<AppUser>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            var plaintextEmail = entry.Entity.PlaintextEmailForIndexing;
            if (!string.IsNullOrEmpty(plaintextEmail))
            {
                if (!plaintextEmail.Contains("template.internal"))
                {
                    entry.Entity.EmailIndex =
                        _encryptor.GenerateBlindIndex(plaintextEmail.Trim().ToUpperInvariant(), _currentVersion);
                }
            }

            var plaintextUsername = entry.Entity.PlaintextUsernameForIndexing;
            if (!string.IsNullOrEmpty(plaintextUsername))
            {
                if (!plaintextUsername.Contains("template.internal"))
                {
                    entry.Entity.UsernameIndex =
                        _encryptor.GenerateBlindIndex(plaintextUsername.Trim().ToUpperInvariant(), _currentVersion);
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.Flagged = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}