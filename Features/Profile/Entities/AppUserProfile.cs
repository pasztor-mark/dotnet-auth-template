using System.ComponentModel.DataAnnotations.Schema;
using auth_template.Entities.Data;
using auth_template.Entities.Interfaces;

namespace auth_template.Features.Profile.Entities;

public class AppUserProfile : IAnonymizable
{
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public virtual AppUser User { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string Bio { get; set; } = "";
    public string Headline { get; set; } = "";

    public string Location { get; set; } = "";


    public bool IsPublic { get; set; } = true;

    public bool Flagged { get; set; }
    public DateTime? AnonymizedAt { get; set; }

    public void Anonymize()
    {
        this.Flagged = true;
        this.AnonymizedAt = DateTime.UtcNow;
        this.IsPublic = false;
        this.Bio = "This user has been removed";
        this.DateOfBirth = null;
        this.Headline = "";
        this.Location = "";
    }

    public void Reactivate(string displayName)
    {
        this.IsPublic = true;
        this.AnonymizedAt = null;
        this.Bio = "";
    }

    public AppUserProfile()
    {
    }

    public AppUserProfile(AppUser user)
    {
        this.UserId = user.Id;
    }    
    public AppUserProfile(Guid userId)
    {
        this.UserId = userId;
    }
}