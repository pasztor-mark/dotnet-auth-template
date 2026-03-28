using auth_template.Entities;
using auth_template.Entities.Configuration;
using auth_template.Features.Admin.Responses;
using auth_template.Features.Auth.Enums;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Utilities.Permissions;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Email.Utilities;
using auth_template.Features.Email.Utilities.Client;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace auth_template.Features.Admin.Services.Dashboard;

public class AdminDashboardService(
    IPermissionUtility _perms,
    AppDbContext _ctx,
    IMemoryCache _cache,
    IUserUtils _userUtils,
    IAuditUtility _audit,
    ILogger<AdminDashboardService> _logger,
    IEmailSenderClient _email) : IAdminDashboardService
{
    private const string CacheKey = "AdminDashboardSummary";

    private readonly string frontendUrl =
        Environment.GetEnvironmentVariable("FrontendUrl")?.TrimEnd('/') ?? "platform.template.local";

    public async Task<LogicResult<List<SelfResponse>>> GetUsersByTagName(string tagName)
    {
        var tag = _perms.GetTagGuidByName(tagName);
        if (tag is null)
        {
            _logger.LogWarning("GetUsersByTagName failed: Tag '{TagName}' does not exist or returned null.", tagName);
            return LogicResult<List<SelfResponse>>.NotFound("This tag does not exist.");
        }

        try
        {
            var userIds = await _perms.GetUsersOfTagAsync(tag.Value);

            if (userIds == null || !userIds.Any())
            {
                _logger.LogInformation("GetUsersByTagName: No users found for tag '{TagName}' ({TagId}).", tagName, tag.Value);
            }

            var userList = await _ctx.Users
                .AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .ToListAsync();

            var userTasks = userList.Select(async user =>
            {
                var permissions = await _perms.GetFullPermissionsAsync(user.Id);
                return new SelfResponse(user, permissions);
            });

            var users = (await Task.WhenAll(userTasks)).ToList();

            return LogicResult<List<SelfResponse>>.Ok(users);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred in GetUsersByTagName for tag '{TagName}'.", tagName);
            return LogicResult<List<SelfResponse>>.Error(e.Message);
        }
    }

    public async Task<LogicResult<UserGrowthStats>> GetUserGrowthAsync(CancellationToken ct)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var totalActive = await _ctx.Users
                .AsNoTracking()
                .CountAsync(u => u.BannedAt == null && u.AnonymizedAt == null, ct);

            var newToday = await _ctx.UserTagAssignments
                .AsNoTracking()
                .Where(a => a.AssignedAt >= today)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(ct);

            var newWeek = await _ctx.UserTagAssignments
                .AsNoTracking()
                .Where(a => a.AssignedAt >= weekAgo)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(ct);

            var distRaw = await _ctx.UserTagAssignments
                .AsNoTracking()
                .Include(a => a.Tag)
                .GroupBy(a => a.Tag.Name)
                .Select(g => new { Name = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
                .ToListAsync(ct);

            var distByTag = distRaw.ToDictionary(x => x.Name, x => x.Count);

            var distByTier = distRaw
                .Where(x => x.Name == "ProTier" || x.Name == "PremiumTier" || x.Name == "Member")
                .ToDictionary(x => x.Name, x => x.Count);

            return LogicResult<UserGrowthStats>.Ok(new UserGrowthStats(totalActive, newToday, newWeek, distByTag, distByTier));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred in GetUserGrowthAsync.");
            return LogicResult<UserGrowthStats>.Error(e.Message);
        }
    }

    public async Task<LogicResult<EngagementStats>> GetEngagementAsync(CancellationToken ct)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var dau = await _ctx.UserActivities
                .AsNoTracking()
                .Where(a => a.Timestamp >= today)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(ct);

            var wau = await _ctx.UserActivities
                .AsNoTracking()
                .Where(a => a.Timestamp >= weekAgo)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(ct);

            var avgSec = await _ctx.UserActivities
                .AsNoTracking()
                .Where(a => a.Timestamp >= weekAgo)
                .AverageAsync(a => (double?)a.TotalSeconds, ct) ?? 0;

            var popularRaw = await _ctx.UserActivities
                .AsNoTracking()
                .GroupBy(a => a.PageKey)
                .Select(g => new
                {
                    PageKey = g.Key,
                    TotalSeconds = g.Sum(x => x.TotalSeconds),
                    HitCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSeconds)
                .Take(10)
                .ToListAsync(ct);

            var popular = popularRaw
                .Select(x => new PageActivityDetail(x.PageKey, x.TotalSeconds, x.HitCount))
                .ToList();

            return LogicResult<EngagementStats>.Ok(new EngagementStats(dau, wau, avgSec, popular));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred in GetEngagementAsync.");
            return LogicResult<EngagementStats>.Error(e.Message);
        }
    }

    public async Task<LogicResult<SubscriptionStats>> GetSubscriptionStatsAsync(CancellationToken ct)
    {
        try
        {
            var proTierCount = await _ctx.UserTagAssignments
                .AsNoTracking()
                .CountAsync(a => a.TagId == TagConstants.Tags.ProTier, ct);

            var premiumTierCount = await _ctx.UserTagAssignments
                .AsNoTracking()
                .CountAsync(a => a.TagId == TagConstants.Tags.PremiumTier, ct);

            var totalUsers = await _ctx.Users
                .AsNoTracking()
                .CountAsync(u => u.BannedAt == null && u.AnonymizedAt == null, ct);

            double conversionRate = totalUsers > 0 
                ? (double)(proTierCount + premiumTierCount) / totalUsers * 100 
                : 0;

            double projectedRevenue = (proTierCount * 19.99) + (premiumTierCount * 49.99);

            return LogicResult<SubscriptionStats>.Ok(new SubscriptionStats(proTierCount, premiumTierCount, Math.Round(conversionRate, 2), Math.Round(projectedRevenue, 2)));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred in GetSubscriptionStatsAsync.");
            return LogicResult<SubscriptionStats>.Error(e.Message);
        }
    }

    public async Task<LogicResult<SystemHealthStats>> GetSystemHealthAsync(CancellationToken ct)
    {
        try
        {
            var flagged = await _ctx.Users
                .AsNoTracking()
                .CountAsync(u => u.Flagged, ct);

            var banned = await _ctx.Users
                .AsNoTracking()
                .CountAsync(u => u.BannedAt != null, ct);

            var failedLogins = await _ctx.Users
                .AsNoTracking()
                .SumAsync(u => u.AccessFailedCount, ct);

            var pendingActions = await _ctx.Users
                .AsNoTracking()
                .CountAsync(u => u.Flagged && u.BannedAt == null, ct);

            return LogicResult<SystemHealthStats>.Ok(new SystemHealthStats(flagged, banned, failedLogins, pendingActions));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred in GetSystemHealthAsync.");
            return LogicResult<SystemHealthStats>.Error(e.Message);
        }
    }

    public async Task<LogicResult<AdminDashboardSummaryResponse>> GetFullDashboardSummaryAsync(CancellationToken ct)
    {
        try
        {
            if (_cache.TryGetValue(CacheKey, out AdminDashboardSummaryResponse cachedResponse))
            {
                _logger.LogInformation("Returning AdminDashboardSummary from cache.");
                return LogicResult<AdminDashboardSummaryResponse>.Ok(cachedResponse);
            }

            var userGrowthResult = await GetUserGrowthAsync(ct);
            if (userGrowthResult.data == null)
                _logger.LogWarning("GetFullDashboardSummaryAsync: Failed to retrieve UserGrowthStats. Error: {Error}", userGrowthResult.message);

            var engagementResult = await GetEngagementAsync(ct);
            if (engagementResult.data == null)
                _logger.LogWarning("GetFullDashboardSummaryAsync: Failed to retrieve EngagementStats. Error: {Error}", engagementResult.message);

            var subscriptionResult = await GetSubscriptionStatsAsync(ct);
            if (subscriptionResult.data == null)
                _logger.LogWarning("GetFullDashboardSummaryAsync: Failed to retrieve SubscriptionStats. Error: {Error}", subscriptionResult.message);

            var healthResult = await GetSystemHealthAsync(ct);
            if (healthResult.data == null)
                _logger.LogWarning("GetFullDashboardSummaryAsync: Failed to retrieve SystemHealthStats. Error: {Error}", healthResult.message);

            var response = new AdminDashboardSummaryResponse
            {
                UserGrowth = userGrowthResult.data,
                Engagement = engagementResult.data,
                Subscriptions = subscriptionResult.data,
                Health = healthResult.data,
                GeneratedAt = DateTime.UtcNow
            };

            _cache.Set(CacheKey, response, TimeSpan.FromHours(1));
            _logger.LogInformation("AdminDashboardSummary successfully generated and cached.");

            return LogicResult<AdminDashboardSummaryResponse>.Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred in GetFullDashboardSummaryAsync.");
            return LogicResult<AdminDashboardSummaryResponse>.Error(e.Message);
        }
    }

    public async Task<LogicResult<AuditLogResponse>> GetUserAuditLogsAsync(string userName, CancellationToken ct)
    {
        var user = await _userUtils.GetAndUpgradeUserByUsernameAsync(userName);
        if (user is null) return LogicResult<AuditLogResponse>.NotFound("No User found by this username.");
        
        try
        {
            var avatarUrl = await _ctx.UserProfiles.AsNoTracking().Where(u => u.UserId == user.Id).Select(u => u.AvatarUrl).FirstOrDefaultAsync(ct);
            var updates = await _ctx.UserUpdates.Where(u => u.UserId == user.Id).ToListAsync(ct);

            var ret = new AuditLogResponse(user, avatarUrl, updates.Select(u => new AuditLogItemResponse(u)).ToList());
            return LogicResult<AuditLogResponse>.Ok(ret);
        }
        catch (Exception e)
        {
            return LogicResult<AuditLogResponse>.Error("Something went wrong.");
        }
    }
}