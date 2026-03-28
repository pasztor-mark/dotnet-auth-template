using System.Security.Claims;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Utilities.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace auth_template.Features.Auth.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class OwnerOrPermission(string _permission) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionUtility>();
        
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var routeValues = context.RouteData.Values;
        object? targetRaw = routeValues["id"] ?? routeValues["username"] ?? routeValues["userId"] ?? routeValues["usn"];

        if (targetRaw == null || string.IsNullOrWhiteSpace(targetRaw.ToString()))
        {
            var pathSegments = context.HttpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments != null)
            {
                for (int i = 0; i < pathSegments.Length; i++)
                {
                    if (pathSegments[i].Equals("u", StringComparison.OrdinalIgnoreCase) && i + 1 < pathSegments.Length)
                    {
                        targetRaw = pathSegments[i + 1];
                        break;
                    }
                }
            }
        }

        if (targetRaw != null)
        {
            var targetStr = targetRaw.ToString();

            if (Guid.TryParse(targetStr, out var targetGuid))
            {
                if (targetGuid == currentUserId)
                {
                    return;
                }
            }

            var currentUsername = context.HttpContext.User.Identity?.Name;
            var normalizedTarget = targetStr!.Trim().ToUpperInvariant();

            if (!string.IsNullOrEmpty(currentUsername) && 
                normalizedTarget.Equals(currentUsername.ToUpperInvariant()))
            {
                return;
            }

            var userManager = context.HttpContext.RequestServices.GetService<UserManager<AppUser>>();
            if (userManager != null)
            {
                var currentUser = await userManager.FindByIdAsync(currentUserId.ToString());
                if (currentUser != null)
                {
                    var dbUsername = currentUser.NormalizedUserName ?? currentUser.UserName?.ToUpperInvariant();
                    if (normalizedTarget.Equals(dbUsername))
                    {
                        return;
                    }
                }
            }
        }

        var userFeatures = await permissionService.GetUserFeaturesAsync(currentUserId);
        if (userFeatures.Contains(_permission))
        {
            return;
        }

        context.Result = new ForbidResult();
    }
}