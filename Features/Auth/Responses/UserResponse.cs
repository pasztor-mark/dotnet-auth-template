using auth_template.Entities.Data;

namespace auth_template.Features.Auth.Responses;

public class UserResponse
{
    public UserResponse()
    {
    }

    public UserResponse(AppUser user)
    {
        UserId = user.Id;
        UserName = user.UserName;
        EmailConfirmed = user.EmailConfirmed;
        EmailAddress = user.Email;
        this.Flagged = user.Flagged;
    }

    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string EmailAddress { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool Flagged { get; set; }
}