namespace auth_template.Utilities;

public class LogicResult<T>
{
    public LogicResult(T? data, string? message, int statusCode, string[]? errors = null)
    {
        this.data = data;
        this.statusCode = statusCode;
        this.message = message;
        this.errors = errors;
    }

    public LogicResult(T? data, int statusCode)
    {
        this.statusCode = statusCode;
        this.data = data;
        this.errors = null;
    }

    public LogicResult(int statusCode)
    {
        this.message = null;
        this.statusCode = statusCode;
        this.errors = null;
    }

    public LogicResult(string? message, int statusCode)
    {
        this.message = message;
        this.statusCode = statusCode;
        this.errors = null;
    }

    public T? data { get; }
    public string? message { get; }
    public int? statusCode { get; }
    public string[]? errors { get; }

    public void Deconstruct(out T? data, out string? message, out int? statusCode)
    {
        data = this.data;
        message = this.message;
        statusCode = this.statusCode;
    }

    public LogicResult() { }

    public static LogicResult<T> Ok(T data) => new(data, 200);
    public static LogicResult<T> Created(T data) => new(data, 201);
    public static LogicResult<T> NoContent() => new(204);
    
    public static LogicResult<T> NotFound(string? msg = null, string[]? errors = null) => new(default, msg, 404, errors);
    public static LogicResult<T> Unauthorized(string? msg = null, string[]? errors = null) => new(default, msg, 403, errors);
    public static LogicResult<T> Unauthenticated(string? msg = null, string[]? errors = null) => new(default, msg, 401, errors);
    public static LogicResult<T> Conflict(string? msg = null, string[]? errors = null) => new(default, msg, 409, errors);
    public static LogicResult<T> BadRequest(string? msg = null, string[]? errors = null) => new(default, msg, 400, errors);
    public static LogicResult<T> Error(string? msg = null, string[]? errors = null) => new(default, msg, 500, errors);
}


public class LogicResult
{
    public LogicResult(dynamic data, string? message, int statusCode, string[]? errors = null)
    {
        this.data = data;
        this.statusCode = statusCode;
        this.message = message;
        this.errors = errors;
    }

    public LogicResult(dynamic data, int statusCode)
    {
        this.statusCode = statusCode;
        this.data = data;
        this.errors = null;
    }

    public LogicResult(int statusCode)
    {
        this.message = null;
        this.statusCode = statusCode;
        this.errors = null;
    }

    public LogicResult(string? message, int statusCode)
    {
        this.message = message;
        this.statusCode = statusCode;
        this.errors = null;
    }

    public dynamic data { get; }
    public string? message { get; }
    public int? statusCode { get; }
    public string[]? errors { get; }

    public LogicResult() { }

    public static LogicResult Ok(dynamic data) => new(data, 200);
    public static LogicResult Created(dynamic data) => new(data, 201);
    public static LogicResult NoContent() => new(204);
    
    public static LogicResult NotFound(string? msg = null, string[]? errors = null) => new(null, msg, 404, errors);
    public static LogicResult Unauthorized(string? msg = null, string[]? errors = null) => new(null, msg, 401, errors);
    public static LogicResult Unauthenticated(string? msg = null, string[]? errors = null) => new(null, msg, 403, errors);
    public static LogicResult Conflict(string? msg = null, string[]? errors = null) => new(null, msg, 409, errors);
    public static LogicResult BadRequest(string? msg = null, string[]? errors = null) => new(null, msg, 400, errors);
    public static LogicResult RateLimited() => new("You are being rate limited.",  statusCode: 429);
    public static LogicResult Error(string? msg = null, string[]? errors = null) => new(null, msg, 500, errors);
}