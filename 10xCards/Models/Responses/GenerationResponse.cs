namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for AI flashcard generation
/// Maps to: POST /api/generations
/// Based on: Generation entity with proposed flashcards
/// </summary>
public sealed class GenerationResponse {
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
	public List<ProposedFlashcardDto> Flashcards { get; set; } = new();
}

