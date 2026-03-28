namespace auth_template.Features.Admin.Responses;

public record UserGrowthStats(
    int TotalActiveUsers,
    int NewUsersToday,
    int NewUsersThisWeek,
    Dictionary<string, int> UserDistributionByTag,
    Dictionary<string, int> UserDistributionByTier
);

public record EngagementStats(
    int DailyActiveUsers,
    int WeeklyActiveUsers,
    double AverageSecondsOnPlatform,
    List<PageActivityDetail> PopularPages
);

public record PageActivityDetail(string PageKey, int TotalSeconds, int HitCount);

public record SubscriptionStats(
    int TotalProUsers,
    int TotalPremiumUsers,
    double ConversionRate,
    double ProjectedMonthlyRevenue
);

public record SystemHealthStats(
    int FlaggedUsersCount,
    int BannedUsersCount,
    int FailedLoginAttemptsTotal,
    int PendingSupportActionsCount
);
public record PlatformEngagementStats(
    int DailyActiveUsers,
    int WeeklyActiveUsers,
    double AverageSecondsOnPlatform,
    List<PageActivityDetail> PopularPages
);

public class AdminDashboardSummaryResponse
{
    public UserGrowthStats? UserGrowth { get; init; }
    public EngagementStats? Engagement { get; init; }
    public SubscriptionStats? Subscriptions { get; init; }
    public SystemHealthStats? Health { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}