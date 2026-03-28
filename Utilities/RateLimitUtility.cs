using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace auth_template.Utilities;

public static class RateLimitUtility
{
    public static ValueTask OnRateLimitRejection(OnRejectedContext context, CancellationToken token)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        ActionResult payload = ResponseUtility.HttpResponse(LogicResult.RateLimited());
        string json = JsonSerializer.Serialize(payload);

        return new ValueTask(context.HttpContext.Response.WriteAsync(json, token));
    }
}