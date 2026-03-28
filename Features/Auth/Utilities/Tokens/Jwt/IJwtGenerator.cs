using auth_template.Entities.Data;
using auth_template.Features.Auth.Responses.Permissions;

namespace auth_template.Features.Auth.Utilities.Tokens.Jwt;

public interface IJwtGenerator
{
    Task<string> GenerateTokenAsync(AppUser user, string userAgentIndex,  int version, PermissionTransfer? permissions = null);
}