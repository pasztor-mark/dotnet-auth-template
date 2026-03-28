using Microsoft.AspNetCore.Mvc;

namespace auth_template.Utilities;

public static class ResponseUtility<T>
{
    public static ActionResult<T?> HttpResponse(LogicResult<T?> res)
    {
        return new ObjectResult(res)
        {
            StatusCode = res.statusCode ?? 500,
        };
    }
}
public static class ResponseUtility
{
    public static ActionResult HttpResponse(LogicResult res)
    {
        return new ObjectResult(res)
        {
            
            StatusCode = res.statusCode ?? 500,
        };
    }
}