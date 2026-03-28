using System.Reflection;
using auth_template.Entities;
using auth_template.Entities.Configuration;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Responses.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace auth_template.Features.Auth.Utilities.Permissions;

public class PermissionUtility(AppDbContext _ctx, IMemoryCache _cache, ILogger<PermissionUtility> _logger) : IPermissionUtility
{
    public async Task<List<string>> GetUserFeaturesAsync(Guid userId)
    {
        string cacheKey = $"user_features_{userId}";

        if (!_cache.TryGetValue(cacheKey, out List<string> features))
        {
            features = await FetchFeaturesFromDb(userId);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSize(1);

            _cache.Set(cacheKey, features, cacheOptions);
        }

        return features ?? new List<string>();
    }

    public async Task<bool> AssignTagsToUserAsync(Guid targetUserId, IEnumerable<Guid> tags, string? reason, Guid? assignedBy = null)
    {
        var existingTags = await _ctx.UserTagPermissions.Where(utp => utp.UserId.Equals(targetUserId))
            .Select(utp => utp.TagId).ToListAsync();

        var newTags = tags.Except(existingTags).ToList();
        if (!newTags.Any())
        {
            _logger.LogInformation("No tags have been assigned to user {uid}", targetUserId);
        }

        try
        {
            foreach (Guid tag in newTags)
            {
                _ctx.UserTagPermissions.Add(new AppUserTagPermissions
                {
                    UserId = targetUserId,
                    TagId = tag
                });

                _ctx.UserTagAssignments.Add(new AppUserTagAssignment
                {
                    UserId = targetUserId,
                    TagId = tag,
                    AssignedAt = DateTime.UtcNow,
                    Reason = reason ?? "Unknown",
                    AssignedById = assignedBy
                });
            
                _cache.Remove($"users_of_tag_{tag}");
            }

            await _ctx.SaveChangesAsync();
            this.InvalidateCache(targetUserId);
            _logger.LogInformation("Tags [{tags}] assigned to user {uid}", string.Join(", ", tags), targetUserId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error assigning tags to user {uid}", targetUserId);
        }
        return false;
    }
    public async Task<List<Guid>> GetUserTagIdsAsync(Guid userId)
    {
        string cacheKey = $"user_tag_ids_{userId}";

        if (!_cache.TryGetValue(cacheKey, out List<Guid>? tagIds))
        {
            tagIds = await _ctx.UserTagPermissions
                .AsNoTracking()
                .Where(utp => utp.UserId == userId)
                .Select(utp => utp.TagId)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSize(1);

            _cache.Set(cacheKey, tagIds, cacheOptions);
        }

        return tagIds ?? new List<Guid>();
    }
    public void InvalidateCache(Guid userId)
    {
        _cache.Remove($"user_features_{userId}");
        _cache.Remove($"user_tag_ids_{userId}");
        _cache.Remove($"full_user_perms_{userId}");
    }

    public async Task<PermissionTransfer> GetFullPermissionsAsync(Guid userId)
    {
        string cacheKey = $"full_user_perms_{userId}";

        if (!_cache.TryGetValue(cacheKey, out PermissionTransfer? model))
        {
            model = await FetchFullPermissionsFromDb(userId);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSize(1);

            _cache.Set(cacheKey, model, cacheOptions);
        }

        return model ?? new PermissionTransfer();
    }
    public async Task<bool> UpdateUserTagsAsync(Guid targetUserId, IEnumerable<Guid> tagsToAdd, IEnumerable<Guid> tagsToRemove, string? reason, Guid? modifiedBy = null)
{
    try
    {
        var currentTags = await _ctx.UserTagPermissions
            .Where(utp => utp.UserId == targetUserId)
            .ToListAsync();

        var actualToRemove = currentTags
            .Where(utp => tagsToRemove.Contains(utp.TagId))
            .ToList();

        var actualToAdd = tagsToAdd
            .Distinct()
            .Where(tid => !currentTags.Any(ct => ct.TagId == tid))
            .ToList();

        if (!actualToRemove.Any() && !actualToAdd.Any())
        {
            return true;
        }

        if (actualToRemove.Any())
        {
            _ctx.UserTagPermissions.RemoveRange(actualToRemove);
            foreach (var removed in actualToRemove)
            {
                _cache.Remove($"users_of_tag_{removed.TagId}");
            }
        }

        foreach (var tagId in actualToAdd)
        {
            _ctx.UserTagPermissions.Add(new AppUserTagPermissions
            {
                UserId = targetUserId,
                TagId = tagId
            });

            _ctx.UserTagAssignments.Add(new AppUserTagAssignment
            {
                UserId = targetUserId,
                TagId = tagId,
                AssignedAt = DateTime.UtcNow,
                Reason = reason ?? "Admin update",
                AssignedById = modifiedBy
            });
            
            _cache.Remove($"users_of_tag_{tagId}");
        }

        await _ctx.SaveChangesAsync();

        this.InvalidateCache(targetUserId);

        _logger.LogInformation("Tags updated for user {uid}. Added: {added}, Removed: {removed}", 
            targetUserId, string.Join(", ", actualToAdd), string.Join(", ", actualToRemove.Select(x => x.TagId)));

        return true;
    }
    catch (Exception e)
    {
        _logger.LogError(e, "Error during combined tag update for user {uid}", targetUserId);
        return false;
    }
}
    public Guid? GetTagGuidByName(string tagName)
    {
        var tagType = typeof(TagConstants.Tags);
    
        var field = tagType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(f => f.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

        if (field == null) return null;

        return (Guid)field.GetValue(null)!;
    }
    public async Task<List<Guid>> GetUsersOfTagAsync(Guid tagId)
    {
        string cacheKey = $"users_of_tag_{tagId}";

        if (!_cache.TryGetValue(cacheKey, out List<Guid>? userIds))
        {
            userIds = await _ctx.UserTagPermissions
                .AsNoTracking()
                .Where(utp => utp.TagId == tagId)
                .Select(utp => utp.UserId)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSize(1);

            _cache.Set(cacheKey, userIds, cacheOptions);
        }

        return userIds ?? new List<Guid>();
    }

    public async Task<List<Guid>> GetUsersByTagNameAsync(string tagId)
    {
        var tag = this.GetTagGuidByName(tagId);
        if (tag is null) return [];
        return await this.GetUsersOfTagAsync(tag.Value);
    }

    private async Task<PermissionTransfer> FetchFullPermissionsFromDb(Guid userId)
    {
        var data = await _ctx.UserTagPermissions
            .AsNoTracking()
            .Where(utp => utp.UserId == userId)
            .Select(utp => new
            {
                TagName = utp.Tag.Name,
                PermissionNames = utp.Tag.TagPermissions.Select(tp => tp.Permission.Name)
            })
            .ToListAsync();

        return new PermissionTransfer
        {
            Tags = data.Select(d => d.TagName).ToList(),
            Features = data.SelectMany(d => d.PermissionNames).Distinct().ToList()
        };
    }

    private async Task<List<string>> FetchFeaturesFromDb(Guid userId)
    {
        return await _ctx.UserTagPermissions
            .AsNoTracking()
            .Where(utp => utp.UserId == userId)
            .SelectMany(utp => utp.Tag.TagPermissions)
            .Select(tp => tp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }
}