using auth_template.Features.Profile.Entities;

namespace auth_template.Features.Profile.Utilities;

public interface IProfileUtility
{
    Task<AppUserProfile?> GetProfileByUserIdAsync(Guid userId, bool withTracking = false);
    Task<AppUserProfile?> GetProfileByUserNameAsync(string username, bool withTracking = false, bool ignoreVisibility = false);
}