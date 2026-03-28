using System.Net;
using System.Text.Json;
using auth_template.Responses;
using Microsoft.AspNetCore.WebUtilities;

namespace auth_template.Middleware.ErrorHandler;

public class ErrorHandlerMiddleware(RequestDelegate _next, ILogger<ErrorHandlerMiddleware> _logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);

            if (ctx.Response.StatusCode >= 400 && !ctx.Response.HasStarted)
            {
                string[]? errorList = null; 

                var response = new Response<dynamic>
                {
                    data = null,
                    message = ReasonPhrases.GetReasonPhrase(ctx.Response.StatusCode),
                    statusCode = ctx.Response.StatusCode,
                    errors = errorList
                };

                ctx.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(response);
                await ctx.Response.WriteAsync(json);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception");

            if (!ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                ctx.Response.ContentType = "application/json";

                var response = new Response<dynamic>()
                {
                    data = null,
                    message = "An unexpected error occurred",
                    statusCode = ctx.Response.StatusCode,
                    errors = null
                };

                var json = JsonSerializer.Serialize(response);
                await ctx.Response.WriteAsync(json);
            }
        }
    }
}