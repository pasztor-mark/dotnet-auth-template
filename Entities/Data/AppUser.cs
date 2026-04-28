using System.ComponentModel.DataAnnotations.Schema;
using auth_template.Entities.Interfaces;
using auth_template.Features.Auth.Transfer;
using auth_template.Features.Profile.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Entities.Data;

[Index(nameof(EmailIndex))]
[Index(nameof(UsernameIndex))]
[Index(nameof(FullNameIndex))]
[Index(nameof(DisplayName))]
public class AppUser : IdentityUser<Guid>, IAnonymizable
{
    public string EmailIndex { get; set; } = null!;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public string UsernameIndex { get; set; } = null!;
    public bool Flagged { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FullNameIndex { get; set; } = null!;
    
    public DateTime? AnonymizedAt { get; set; }
    public virtual AppUserProfile Profile { get; set; }
    public string? AvatarUrl { get; set; }
    [NotMapped] public string? PlaintextEmailForIndexing { get; set; }
    [NotMapped] public string? PlaintextFullNameForIndexing { get; set; }
    [NotMapped] public string? PlaintextUsernameForIndexing { get; set; }

    public DateTime? BannedAt { get; set; }
    public string? BanReason { get; set; }

    public void Anonymize()
    {
        string fakeEmail = $"{this.Id}@template.internal";
        string fakeName = $"{this.Id}-template.internal";
        this.UserName = fakeName;
        this.NormalizedUserName = fakeName.ToUpperInvariant().Trim();
        this.Email = fakeEmail;
        this.NormalizedEmail = fakeEmail.ToUpperInvariant().Trim();
        this.Flagged = true;
        this.AnonymizedAt = DateTime.UtcNow;
        this.DisplayName = "Deactivated Account";
        this.AvatarUrl = null;
    }

    public void Reactivate(ReactivateAccountDto dto)
    {
        string normalizedEmail = dto.Email.ToUpperInvariant().Trim();
        string normalizedUserName = dto.Username.ToUpperInvariant().Trim();

        this.Email = dto.Email;
        this.NormalizedEmail = normalizedEmail;
        this.AnonymizedAt = null;
        this.Flagged = false;
        this.DisplayName = dto.DisplayName;
        this.PlaintextFullNameForIndexing = dto.DisplayName;
        this.UserName = dto.Username;
        this.NormalizedUserName = normalizedUserName;
        this.PlaintextEmailForIndexing = normalizedEmail;
        this.PlaintextUsernameForIndexing = normalizedUserName;
    }

    public void Ban(string? reason = null)
    {
        this.BannedAt = DateTime.UtcNow;
        this.BanReason = reason ?? "Reason not provided";
    }

    public void Unban()
    {
        this.BannedAt = null;
        this.BanReason = null;
    }
}