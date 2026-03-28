using auth_template.Features.Auth.Utilities.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace auth_template.Features.Auth.Attributes;
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class WithPermissions(params string[] _permissions) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionUtility>();
        var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userFeatures = await permissionService.GetUserFeaturesAsync(userId);

        if (!_permissions.Any(f => userFeatures.Contains(f)))
        {
            context.Result = new ForbidResult();
        }
    }
}