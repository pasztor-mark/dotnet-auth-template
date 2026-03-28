using System.Security.Cryptography;

namespace auth_template.Features.Auth.Utilities.Tokens.Refresh;

public class RefreshGenerator : IRefreshGenerator
{
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}