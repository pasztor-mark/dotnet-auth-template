using System.Collections.Concurrent;

namespace auth_template.Features.Auth.Utilities.Activity;

public class ActivityBuffer : IActivityBuffer
{
    private ConcurrentDictionary<ActivityKey, int> _buffer = new();

    public void AddActivity(Guid userId, string pageKey, int seconds)
    {
        var key = new ActivityKey(userId, DateTime.UtcNow.Date, pageKey);
        _buffer.AddOrUpdate(key, seconds, (k, oldSeconds) => oldSeconds + seconds);
    }

    public IDictionary<ActivityKey, int> GetAndClear()
    {
        var oldBuffer = Interlocked.Exchange(ref _buffer, new ConcurrentDictionary<ActivityKey, int>());
        return oldBuffer;
    }
}