using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Responses.Permissions;
using auth_template.Features.Auth.Utilities.Permissions;
using auth_template.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace auth_template.Features.Auth.Utilities.Tokens.Jwt;

public class JwtGenerator(IOptions<SecurityOptions> _options, IPermissionUtility _permissionUtility) : IJwtGenerator
{
    public async Task<string> GenerateTokenAsync(AppUser user, string userAgentIndex, int version,
        PermissionTransfer? permissions)
    {
        ArgumentNullException.ThrowIfNull(user);

        var key = _options.Value.Keys.JwtSigningKey ?? throw new InvalidOperationException("JWT Key missing.");
        var issuerAudiencePair = _options.Value.IssuerAudiencePair.Split(';');
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);

        var symmetricKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

        var perms = permissions ?? await _permissionUtility.GetFullPermissionsAsync(user.Id);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.NormalizedUserName),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("uah", userAgentIndex),
            new("uav", version.ToString())
        };

        claims.AddRange(perms.Tags.Select(tag => new Claim(ClaimTypes.Role, tag)));

        claims.AddRange(perms.Features.Select(feat => new Claim("prm", feat)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(AuthConfiguration.JwtExpirationInSeconds),
            SigningCredentials = creds,
            Issuer = issuerAudiencePair[0],
            Audience = issuerAudiencePair[1]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}