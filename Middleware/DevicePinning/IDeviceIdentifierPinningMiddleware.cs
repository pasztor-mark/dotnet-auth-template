namespace auth_template.Middleware.DevicePinning;

public interface IDeviceIdentifierPinningMiddleware
{
    Task InvokeAsync(HttpContext context);
}