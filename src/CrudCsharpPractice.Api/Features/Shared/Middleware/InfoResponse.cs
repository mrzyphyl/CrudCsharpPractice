namespace CrudCsharpPractice.Api.Features.Shared.Middleware;

public class InfoResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static InfoResponse<T> Ok(T data, string message = "Success", string code = "SUCCESS")
    {
        return new InfoResponse<T>
        {
            Data = data,
            Message = message,
            Code = code
        };
    }

    public static InfoResponse<T> Created(T data, string message = "Resource created", string code = "CREATED")
    {
        return new InfoResponse<T>
        {
            Data = data,
            Message = message,
            Code = code
        };
    }

    public static InfoResponse<T> Deleted(string message = "Resource deleted", string code = "DELETED")
    {
        return new InfoResponse<T>
        {
            Message = message,
            Code = code
        };
    }

    public static InfoResponse<T> WithMetadata(T data, Dictionary<string, object> metadata, string message = "Success", string code = "SUCCESS")
    {
        return new InfoResponse<T>
        {
            Data = data,
            Message = message,
            Code = code,
            Metadata = metadata
        };
    }
}

public static class InfoResponse
{
    public static InfoResponse<object> Success(string message = "Success", string code = "SUCCESS")
    {
        return new InfoResponse<object>
        {
            Message = message,
            Code = code
        };
    }

    public static InfoResponse<object> Deleted(string message = "Resource deleted", string code = "DELETED")
    {
        return new InfoResponse<object>
        {
            Message = message,
            Code = code
        };
    }
}
