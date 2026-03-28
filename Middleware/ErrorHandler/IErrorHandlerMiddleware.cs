namespace auth_template.Middleware.ErrorHandler;

public interface IErrorHandlerMiddleware
{
    Task InvokeAsync(HttpContext ctx);
}