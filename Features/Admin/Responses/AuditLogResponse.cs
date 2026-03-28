using auth_template.Entities.Data;
using auth_template.Features.Auth.Responses;

namespace auth_template.Features.Admin.Responses;

public class AuditLogResponse
{
    public UserListingResponse User { get; set; }
    public List<AuditLogItemResponse> Logs { get; set; }

    public AuditLogResponse()
    {
        
    }

    public AuditLogResponse(AppUser user, string avatarUrl ,List<AuditLogItemResponse> logs)
    {
        this.User = new UserListingResponse(user, avatarUrl);
        this.Logs = logs;
    }
}