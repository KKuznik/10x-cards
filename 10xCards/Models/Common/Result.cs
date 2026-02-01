namespace _10xCards.Models.Common;

/// <summary>
/// Generic result type for service operations
/// Encapsulates operation success/failure with optional value and error details
/// </summary>
/// <typeparam name="T">The type of the value returned on success</typeparam>
public sealed class Result<T> {
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, List<string>>? Errors { get; init; }

    private Result(bool isSuccess, T? value, string? errorMessage, Dictionary<string, List<string>>? errors) {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static Result<T> Success(T value) =>
        new(true, value, null, null);

    /// <summary>
    /// Creates a failed result with a single error message
    /// </summary>
    public static Result<T> Failure(string errorMessage) =>
        new(false, default, errorMessage, null);

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    public static Result<T> Failure(Dictionary<string, List<string>> errors) =>
        new(false, default, null, errors);

    /// <summary>
    /// Creates a failed result with both error message and validation errors
    /// </summary>
    public static Result<T> Failure(string errorMessage, Dictionary<string, List<string>> errors) =>
        new(false, default, errorMessage, errors);
}

