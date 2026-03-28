namespace auth_template.Features.Auth.Utilities.Activity;

public interface IActivityBuffer
{
    void AddActivity(Guid userId, string pageKey, int seconds);
    IDictionary<ActivityKey, int> GetAndClear();
}