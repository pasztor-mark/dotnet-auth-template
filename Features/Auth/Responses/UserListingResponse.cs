using auth_template.Entities.Data;

namespace auth_template.Features.Auth.Responses;

public class UserListingResponse
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string AvatarUrl { get; set; }
    public bool Flagged { get; set; }

    public UserListingResponse()
    {
        
    }

    public UserListingResponse(AppUser u)
    {
        this.UserId = u.Id;
        this.AvatarUrl = u.AvatarUrl;
        this.Flagged = u.Flagged;
        this.DisplayName = u.DisplayName;
        this.UserName = u.NormalizedUserName.ToLower();
    }
}