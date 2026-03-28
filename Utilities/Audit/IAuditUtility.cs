using auth_template.Features.Auth.Enums;

namespace auth_template.Utilities.Audit;

public interface IAuditUtility
{
    Task LogUserActivityAsync(Guid userId, string action, string? description = "", LogType type = LogType.General);
}