using System.IdentityModel.Tokens.Jwt;
using auth_template.Features.Auth.Configuration;

namespace auth_template.Features.Auth.Utilities.Tokens.Jwt;

public class TokenUtility(IHttpContextAccessor _http) : ITokenUtility
{
    public void SetTokenCookies(string accessToken, string? refreshToken)
    {
        HttpContext? context = _http.HttpContext;
        if (context is null) return;
        context.Response.Cookies.Append("X-Access-Token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddSeconds(AuthConfiguration.JwtExpirationInSeconds),
            Path = "/" 
            
        });

        if (!string.IsNullOrEmpty(refreshToken))
        {
            context.Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMonths(AuthConfiguration.RefreshTokenExpirationInMonths),
                Path = "/" 
                
            });
        }
    }

    public bool TokenIsExpired(string token)
    {
        try
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtHandler.ReadJwtToken(token);
            return jwtToken.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public void ClearTokens(bool clearRefresh = false, bool clearAccess = false)
    {
        HttpContext? context = _http.HttpContext;
        if (context is null) return;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/" 
        };

        if (clearAccess)
        {
            context.Response.Cookies.Append("X-Access-Token", "", cookieOptions);
        }

        if (clearRefresh)
        {
            context.Response.Cookies.Append("X-Refresh-Token", "", cookieOptions);
        }
    }
}