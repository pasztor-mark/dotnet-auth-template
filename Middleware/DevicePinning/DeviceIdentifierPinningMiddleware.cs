using System.Security.Claims;
using auth_template.Configuration;
using auth_template.Utilities;
using auth_template.Utilities.Security.Encryption;

namespace auth_template.Middleware.DevicePinning;

public class DeviceIdentifierPinningMiddleware(
    RequestDelegate next,
    IEncryptor _encryptor, ILogger<DeviceIdentifierPinningMiddleware> _logger) : IDeviceIdentifierPinningMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && DeviceIdentifierConfig.ShouldDisableIdentification(path))
        {
            await next(context);
            return;
        }
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenUaHash = context.User.FindFirst("uah")?.Value;
        var tokenVersionStr = context.User.FindFirst("uav")?.Value;

        if (tokenUaHash != null && int.TryParse(tokenVersionStr, out int tokenVersion))
        {
            var currentUa = context.Request.Headers.UserAgent.ToString();
            var currentUaHash = _encryptor.GenerateBlindIndex(currentUa, tokenVersion); 

            if (tokenUaHash != currentUaHash)
            {
                _logger.LogWarning("Security Alert - Device mismatch detected for {uid}", userId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(LogicResult<dynamic>.Unauthenticated("Device mismatch."));
                return;
            }
        }

        await next(context);
    }
}