using _10xCards.Models.Common;

namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for paginated flashcard list
/// Maps to: GET /api/flashcards
/// </summary>
public sealed class FlashcardsListResponse {
	public List<FlashcardResponse> Data { get; set; } = new();
	public PaginationMetadata Pagination { get; set; } = new();
}

