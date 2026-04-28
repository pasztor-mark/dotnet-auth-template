
using auth_template.Features.Profile.Entities;

namespace auth_template.Features.Profile.Responses;

public class ProfileResponse
{
    public string DisplayName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Bio { get; set; }
    public string Headline { get; set; }
    public string userName  { get; set; }
    public string Location { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsPublic { get; set; } = true;

    public ProfileResponse()
    {
        
    }

    public ProfileResponse(AppUserProfile p)
    {
        this.AvatarUrl = p.User.AvatarUrl;
        this.Bio = p.Bio;
        this.userName = p.User.UserName;
        this.DateOfBirth = p.DateOfBirth;
        this.Headline = p.Headline;
        this.IsPublic = p.IsPublic;
        this.Location = p.Location;
    }
    
    
    
    public void Deconstruct(out string displayName, out DateTime? dateOfBirth, out string bio, out string headline, out string location, out string? avatarUrl, out bool isPublic)
    {
        displayName = DisplayName;
        dateOfBirth = DateOfBirth;
        bio = Bio;
        headline = Headline;
        location = Location;
        avatarUrl = AvatarUrl;
        isPublic = IsPublic;
    }
}