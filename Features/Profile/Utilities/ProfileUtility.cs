using System.Linq.Expressions;
using auth_template.Entities;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Profile.Entities;
using auth_template.Utilities;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Profile.Utilities;

public class ProfileUtility(AppDbContext _ctx, IUserUtils _userUtils) : IProfileUtility
{
    public async Task<AppUserProfile?> GetProfileByUserIdAsync(Guid userId, bool withTracking)
    {
        var query = _ctx.UserProfiles.AsSplitQuery().IgnoreQueryFilters().Include(u => u.User);
        Expression<Func<AppUserProfile, bool>>? expression = p => p.UserId.Equals(userId);
        if (!withTracking)
        {
            return await query.AsNoTracking().FirstOrDefaultAsync(expression);
        }

        return await query.FirstOrDefaultAsync(expression);
    }

    public async Task<AppUserProfile?> GetProfileByUserNameAsync(string username, bool withTracking, bool ignoreVisibility)
    {
        AppUser? user = await _userUtils.GetAndUpgradeUserByUsernameAsync(username);
        if (user is null) return null;
        var query = _ctx.UserProfiles.AsSplitQuery().Include(u => u.User);
        Expression<Func<AppUserProfile, bool>>? expression = p => p.User.Id.Equals(user.Id) && (ignoreVisibility || p.IsPublic);
        if (!withTracking)
        {
            return await query.AsNoTracking().FirstOrDefaultAsync(expression);
        }

        return await query.FirstOrDefaultAsync(expression);
    }
}