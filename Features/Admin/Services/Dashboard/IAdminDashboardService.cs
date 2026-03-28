using auth_template.Features.Admin.Responses;
using auth_template.Features.Auth.Responses;
using auth_template.Utilities;

namespace auth_template.Features.Admin.Services.Dashboard;

public interface IAdminDashboardService
{
    Task<LogicResult<List<SelfResponse>>> GetUsersByTagName(string tagName);
    Task<LogicResult<UserGrowthStats>> GetUserGrowthAsync(CancellationToken ct);
    Task<LogicResult<SystemHealthStats>> GetSystemHealthAsync(CancellationToken ct);
    Task<LogicResult<AdminDashboardSummaryResponse>> GetFullDashboardSummaryAsync(CancellationToken ct);
    Task<LogicResult<AuditLogResponse>> GetUserAuditLogsAsync(string userName, CancellationToken ct);
}