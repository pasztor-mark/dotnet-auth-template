using auth_template.Entities.Configuration;
using auth_template.Enums;
using auth_template.Features.Admin.Responses;
using auth_template.Features.Admin.Services.Dashboard;
using auth_template.Features.Auth.Attributes;
using auth_template.Features.Auth.Responses;
using auth_template.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace auth_template.Features.Admin.Controllers;


[ApiController]
[Authorize]
[Route("api/admin/dashboard")]
public class AdminDashboardController(IAdminDashboardService _dashService) : ControllerBase
{
    [WithPermissions(TagConstants.Users.Update)]
    [HttpGet("users/tag/{tagName}")]
    public async Task<ActionResult<List<SelfResponse>>> GetUsersByTagName([FromRoute] string tagName)
    {
        return ResponseUtility<List<SelfResponse>>.HttpResponse(await _dashService.GetUsersByTagName(tagName));
    }
    [WithPermissions(TagConstants.System.AuditLogs)]
    [HttpGet("user-growth")]
    public async Task<ActionResult<UserGrowthStats>> GetUserGrowthAsync(CancellationToken ct)
    {
        return ResponseUtility<UserGrowthStats>.HttpResponse(await _dashService.GetUserGrowthAsync(ct));
    }


    [WithPermissions(TagConstants.System.AuditLogs)]
    [HttpGet("system-health")]
    public async Task<ActionResult<SystemHealthStats>> GetSystemHealthAsync(CancellationToken ct)
    {
        return ResponseUtility<SystemHealthStats>.HttpResponse(await _dashService.GetSystemHealthAsync(ct));
    }
    [WithPermissions(TagConstants.System.AuditLogs)]
    [HttpGet("summary")]
    public async Task<ActionResult<AdminDashboardSummaryResponse>> GetSummaryAsync(CancellationToken ct)
    {
        return ResponseUtility<AdminDashboardSummaryResponse>.HttpResponse(await _dashService.GetFullDashboardSummaryAsync(ct));
    }
    [WithPermissions(TagConstants.System.AuditLogs)]
    [HttpGet("audit/{username}")]
    public async Task<ActionResult<AuditLogResponse>> GetUserAuditLogsAsync([FromRoute] string userName, CancellationToken ct)
    {
        return ResponseUtility<AuditLogResponse>.HttpResponse(await _dashService.GetUserAuditLogsAsync(userName, ct));
    }
}