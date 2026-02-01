namespace _10xCards.Models.Responses;

/// <summary>
/// Standard error response format for API errors
/// Used across all endpoints for consistent error reporting
/// </summary>
public sealed class ErrorResponse {
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public Dictionary<string, List<string>>? Errors { get; set; }
}

