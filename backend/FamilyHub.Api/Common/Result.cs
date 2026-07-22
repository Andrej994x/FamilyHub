namespace FamilyHub.Api.Common;

/// <summary>
/// Outcome of a service operation without a return value.
/// </summary>
public class Result
{
    public bool Succeeded { get; protected init; }

    public ErrorType? ErrorType { get; protected init; }

    public string? Error { get; protected init; }

    public static Result Success() => new() { Succeeded = true };

    public static Result Failure(ErrorType type, string error) =>
        new() { Succeeded = false, ErrorType = type, Error = error };
}

/// <summary>
/// Outcome of a service operation that returns a value on success.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new Result<T> Failure(ErrorType type, string error) =>
        new() { Succeeded = false, ErrorType = type, Error = error };
}
