using auth_template.Entities.Data;
using auth_template.Features.Auth.Enums;

namespace auth_template.Features.Admin.Responses;

public class AuditLogItemResponse
{
    public string Action { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
    public LogType Type { get; set; }

    public AuditLogItemResponse()
    {
        
    }

    public AuditLogItemResponse(AppUserUpdates upd)
    {
        this.Action = upd.Action;
        this.Description = upd.Description;
        this.Timestamp = upd.Timestamp;
        this.Type = upd.Type;
    }
}