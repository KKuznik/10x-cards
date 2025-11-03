namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for detailed generation information with associated flashcards
/// Maps to: GET /api/generations/{id}
/// Based on: Generation entity with related Flashcard entities
/// </summary>
public sealed class GenerationDetailResponse {
	public long Id { get; set; }
	public Guid UserId { get; set; }
	public string Model { get; set; } = string.Empty;
	public int GeneratedCount { get; set; }
	public int? AcceptedUneditedCount { get; set; }
	public int? AcceptedEditedCount { get; set; }
	public string SourceTextHash { get; set; } = string.Empty;
	public int SourceTextLength { get; set; }
	public int GenerationDuration { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public List<FlashcardResponse> Flashcards { get; set; } = new();
}

