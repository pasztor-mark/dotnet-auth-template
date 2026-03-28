using System.Threading.RateLimiting;
using auth_template.Enums;
using Microsoft.AspNetCore.RateLimiting;

namespace auth_template.Configuration;

public static class RateLimitValues
{
    public static Action<FixedWindowRateLimiterOptions> GetEmailLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 2;
            opts.Window = TimeSpan.FromSeconds(30);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetPasswordChangeLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 20;
            opts.Window = TimeSpan.FromMinutes(5);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetLoginLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 5;
            opts.Window = TimeSpan.FromMinutes(5);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetRegisterLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 3;
            opts.Window = TimeSpan.FromSeconds(1);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetSearchLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 30;
            opts.Window = TimeSpan.FromMinutes(2);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetCreationLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 10;
            opts.Window = TimeSpan.FromHours(1);
            opts.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetItemUpdateLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 10;
            opts.Window = TimeSpan.FromHours(8);
            opts.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetItemDeleteLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 20;
            opts.Window = TimeSpan.FromHours(12);
            opts.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetGeneralLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 100;
            opts.Window = TimeSpan.FromMinutes(1);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetRefreshLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 15;
            opts.Window = TimeSpan.FromMinutes(7);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetUserUpdateLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 3;
            opts.Window = TimeSpan.FromHours(3);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetHeartbeatLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 2;
            opts.Window = TimeSpan.FromSeconds(45);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static Action<FixedWindowRateLimiterOptions> GetProfileUpdateLimiter()
    {
        return opts =>
        {
            opts.PermitLimit = 12;
            opts.Window = TimeSpan.FromMinutes(3);
            opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opts.QueueLimit = 0;
        };
    }

    public static void RegisterAllPolicies(this RateLimiterOptions options)
    {
        options.AddFixedWindowLimiter(nameof(RateLimits.Email), GetEmailLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.PasswordChange), GetPasswordChangeLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.Login), GetLoginLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.Register), GetRegisterLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.Search), GetSearchLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.Creation), GetCreationLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.ItemUpdate), GetItemUpdateLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.ItemDelete), GetItemDeleteLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.General), GetGeneralLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.Refresh), GetRefreshLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.UserUpdate), GetUserUpdateLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.Heartbeat), GetHeartbeatLimiter());
        options.AddFixedWindowLimiter(nameof(RateLimits.ProfileUpdate), GetProfileUpdateLimiter());
    }
}