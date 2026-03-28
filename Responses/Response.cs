using auth_template.Utilities;

namespace auth_template.Responses;

public class Response<T>
{
    public Response(int? statusCode, string? message, T? data = default, string[]? errors = null)
    {
        this.statusCode = statusCode;
        this.message = message;
        this.data = data;
        this.errors = errors;   
    }

    public Response()
    {
        this.statusCode = 200;
        this.data = default;
        this.message = null;
        this.errors = null;
    }

    public Response(LogicResult<T?> logicResult) : this(logicResult.statusCode, logicResult.message, logicResult.data, logicResult.errors) {}
    
    public Response(int? statusCode, T? data = default)
    {
        this.statusCode = statusCode;
        this.message = null;
        this.data = data;
        this.errors = null;
    }

    public Response(int? statusCode, string? message)
    {
        this.statusCode = statusCode;
        this.message = message;
        this.data = default;
        this.errors = null;
    }

    public int? statusCode { get; set; }
    public string? message { get; set; }
    public T? data { get; set; }
    public string[]? errors { get; set; }
}