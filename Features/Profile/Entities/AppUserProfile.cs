using System.ComponentModel.DataAnnotations.Schema;
using auth_template.Entities.Data;
using auth_template.Entities.Interfaces;

namespace auth_template.Features.Profile.Entities;

public class AppUserProfile : IAnonymizable
{
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public virtual AppUser User { get; set; }

    public string DisplayName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Bio { get; set; } = "";
    public string Headline { get; set; } = "";

    public string Location { get; set; } = "";


    public string? AvatarUrl { get; set; }
    public bool IsPublic { get; set; } = true;

    public bool Flagged { get; set; }
    public DateTime? AnonymizedAt { get; set; }

    public void Anonymize()
    {
        this.Flagged = true;
        this.AnonymizedAt = DateTime.UtcNow;
        this.AvatarUrl = null;
        this.IsPublic = false;
        this.Bio = "This user has been removed";
        this.DateOfBirth = null;
        this.DisplayName = "Deactivated Account";
        this.Headline = "";
        this.Location = "";
    }

    public void Reactivate(string displayName)
    {
        this.IsPublic = true;
        this.AnonymizedAt = null;
        this.DisplayName = displayName;
        this.Bio = "";
    }

    public AppUserProfile()
    {
    }

    public AppUserProfile(AppUser user)
    {
        this.UserId = user.Id;
        this.DisplayName = user.NormalizedUserName.ToLower().Trim();
    }    
    public AppUserProfile(AppUser user, Guid userId)
    {
        this.UserId = userId;
        this.DisplayName =  user.NormalizedUserName.ToLower().Trim();
    }
}