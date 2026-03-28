using auth_template.Entities.Data;

namespace auth_template.Features.Auth.Responses;

public class UserUpdateResponse
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public List<UserUpdateItemResponse> Items { get; set; }

    public UserUpdateResponse(AppUser user, List<AppUserUpdates> updates)
    {
        this.UserId = user.Id;
        this.UserName = user.NormalizedUserName.ToLower();
        this.Items = updates.Select(x => new UserUpdateItemResponse(x)).OrderByDescending(x => x.Timestamp).ToList();
    }
}