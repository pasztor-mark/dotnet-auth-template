using auth_template.Entities;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Enums;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Utilities.Audit;

public class AuditUtility(AppDbContext _ctx) : IAuditUtility
{
    public async Task LogUserActivityAsync(Guid userId, string action, string? description = "", LogType type = LogType.General)
    {
        bool exists = await _ctx.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == userId);
        if (!exists) return;
        AppUserUpdates update = new()
        {
            UserId = userId,
            Action = action,
            Description = description,
            Type = type,
            Timestamp = DateTime.UtcNow,
        };
        await _ctx.UserUpdates.AddAsync(update);
    }
}