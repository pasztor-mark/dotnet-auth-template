using auth_template.Features.Auth.Utilities.User;
using auth_template.Utilities.Security.Encryption;

namespace auth_template.Middleware.TokenRefresh;

public interface ITokenRefreshMiddleware
{
    Task InvokeAsync(HttpContext context, IEncryptor encryptor, IUserUtils _userUtils);
}