using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Query parameters for listing flashcards
/// Maps to: GET /api/flashcards
/// </summary>
public sealed class ListFlashcardsQuery {
	[Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
	public int Page { get; set; } = 1;

	[Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
	public int PageSize { get; set; } = 20;

	[RegularExpression("^(ai-full|ai-edited|manual)?$", ErrorMessage = "Source must be 'ai-full', 'ai-edited', or 'manual'")]
	public string? Source { get; set; }

	[RegularExpression("^(createdAt|updatedAt|front)$", ErrorMessage = "SortBy must be 'createdAt', 'updatedAt', or 'front'")]
	public string SortBy { get; set; } = "createdAt";

	[RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be 'asc' or 'desc'")]
	public string SortOrder { get; set; } = "desc";

	[MaxLength(200, ErrorMessage = "Search query must not exceed 200 characters")]
	public string? Search { get; set; }
}

