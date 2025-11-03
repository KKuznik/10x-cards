using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Query parameters for listing generations
/// Maps to: GET /api/generations
/// </summary>
public sealed class ListGenerationsQuery {
	[Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
	public int Page { get; set; } = 1;

	[Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
	public int PageSize { get; set; } = 20;

	[RegularExpression("^(createdAt)$", ErrorMessage = "SortBy must be 'createdAt'")]
	public string SortBy { get; set; } = "createdAt";

	[RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be 'asc' or 'desc'")]
	public string SortOrder { get; set; } = "desc";
}

