namespace _10xCards.Models.Common;

/// <summary>
/// Pagination metadata for list responses
/// Used in: FlashcardsListResponse, GenerationsListResponse
/// Provides information about page navigation and total items
/// </summary>
public sealed class PaginationMetadata {
	public int CurrentPage { get; set; }
	public int PageSize { get; set; }
	public int TotalPages { get; set; }
	public int TotalItems { get; set; }
}

