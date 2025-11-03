namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for individual flashcard operations
/// Maps to: GET /api/flashcards/{id}, POST /api/flashcards, PUT /api/flashcards/{id}
/// Based on: Flashcard entity
/// </summary>
public sealed class FlashcardResponse {
	public long Id { get; set; }
	public string Front { get; set; } = string.Empty;
	public string Back { get; set; } = string.Empty;
	public string Source { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public long? GenerationId { get; set; }
}

