using System.IdentityModel.Tokens.Jwt;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Utilities.Security.Encryption;

namespace auth_template.Middleware.TokenRefresh;

public class TokenRefreshMiddleware(RequestDelegate _next) : ITokenRefreshMiddleware
{
    public async Task InvokeAsync(HttpContext context, IEncryptor encryptor, IUserUtils _userUtils)
    {
        var refreshToken = context.Request.Cookies["X-Refresh-Token"];
        var accessToken = context.Request.Cookies["X-Access-Token"];
        bool shouldRefresh = string.IsNullOrEmpty(accessToken) || IsTokenExpired(accessToken);
        if (shouldRefresh && !string.IsNullOrEmpty(refreshToken))
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            var result = await _userUtils.RefreshWithTokenAsync(refreshToken, userAgent, ipAddress);

            if (result.statusCode == 200 && result.data is not null)
            {
                var token = result.data.AccessToken;
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None, 
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddSeconds(AuthConfiguration.JwtExpirationInSeconds)
                };

                context.Response.Cookies.Append("X-Access-Token", token, cookieOptions);

                context.Request.Headers.Append("Authorization", $"Bearer {token}");

                if (!string.IsNullOrEmpty(result.data.RefreshToken))
                {
                    context.Response.Cookies.Append("X-Refresh-Token", result.data.RefreshToken, new CookieOptions {
                        HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });
                }
            }
        }

        await _next(context);
    }
    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow.AddSeconds(10);
        }
        catch
        {
            return true;
        }
    }
}