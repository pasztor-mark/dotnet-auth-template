using auth_template.Entities.Data;

namespace auth_template.Features.Auth.Responses;

public class UserUpdateItemResponse
{
    public string Type { get; set; }
    public string Action { get; set; }
    public string? Description { get; set; } = "";
    public DateTime Timestamp { get; set; }

    public UserUpdateItemResponse(AppUserUpdates upd)
    {
        this.Timestamp = upd.Timestamp;
        this.Action = upd.Action;
        this.Description = upd.Description;
        this.Type = upd.Type.ToString();
    }
}