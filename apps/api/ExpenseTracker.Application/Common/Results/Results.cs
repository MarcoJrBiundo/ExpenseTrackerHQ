namespace ExpenseTracker.Application.Common.Results;

public class Result
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static Result Ok() => new() { Success = true };

    public static Result Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

public class Result<T>
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public T? Data { get; init; }

    public static Result<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static Result<T> Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}