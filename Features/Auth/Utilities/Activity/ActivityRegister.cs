using auth_template.Entities;
using auth_template.Entities.Data;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Auth.Utilities.Activity;

public class ActivityRegister(IServiceProvider _serviceProvider, IActivityBuffer _buffer, ILogger<ActivityRegister> _logger)  : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var dataMap = _buffer.GetAndClear();
            if (dataMap.Count == 0) continue;

            using var scope = _serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var (key, seconds) in dataMap)
            {
                var affected = await ctx.UserActivities
                    .Where(ua => ua.UserId == key.UserId && 
                                 ua.Date == key.Date && 
                                 ua.PageKey == key.PageKey)
                    .ExecuteUpdateAsync(s => s
                            .SetProperty(a => a.TotalSeconds, a => a.TotalSeconds + seconds)
                            .SetProperty(a => a.LastSeen, DateTime.UtcNow), 
                        stoppingToken);

                if (affected == 0)
                {
                    try
                    {
                        ctx.UserActivities.Add(new AppUserActivity
                        {
                            UserId = key.UserId,
                            Date = key.Date,
                            PageKey = key.PageKey,
                            TotalSeconds = seconds,
                            LastSeen = DateTime.UtcNow,
                            Timestamp = DateTime.UtcNow
                        });
                        await ctx.SaveChangesAsync(stoppingToken);
                    }
                    catch (DbUpdateException) 
                    {
                    }
                }
            }
        }
    }
}