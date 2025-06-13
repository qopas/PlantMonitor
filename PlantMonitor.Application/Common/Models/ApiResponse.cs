namespace PlantMonitor.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> ErrorResult(string error)
        => new() { Success = false, Error = error };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResult(string? message = null)
        => new() { Success = true, Message = message };

    public static new ApiResponse ErrorResult(string error)
        => new() { Success = false, Error = error };
}
