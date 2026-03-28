using auth_template.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace auth_template.Responses;

public static class ResponseFactory
{
    public static IActionResult ToHttpResult<T>(LogicResult<T> result, Func<T, object>? map = null)
    {
        var data = result.data is not null && map is not null ? map(result.data) : result.data;
        return new ObjectResult(new Response<object>(result.statusCode ?? 500, result.message, data))
        {
            StatusCode = result.statusCode ?? 500
        };
    }
}