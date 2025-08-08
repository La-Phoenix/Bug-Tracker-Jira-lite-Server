namespace BugTrackr.Application.Common;

public class ApiResponse
{
    public bool Success { get; set; } = true;
    public int StatusCode { get; set; } = 200;
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; } = new List<string>();
}
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, int statusCode = 200, string? message = null)
        => new()
        {
            Data = data,
            StatusCode = statusCode,
            Message = message
        };

    public static ApiResponse<T> Failure(string message, int statusCode, IEnumerable<string>? errors = null)
        => new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors
        };

    public static ApiResponse<T> Failure(string message, int statusCode, string error)
        => new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = new List<string> { error }
        };

    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
