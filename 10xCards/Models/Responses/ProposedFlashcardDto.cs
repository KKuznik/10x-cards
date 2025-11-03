namespace _10xCards.Models.Responses;

/// <summary>
/// Proposed flashcard from AI generation (not yet saved to database)
/// Used in: GenerationResponse
/// </summary>
public sealed class ProposedFlashcardDto {
	public string Front { get; set; } = string.Empty;
	public string Back { get; set; } = string.Empty;
}

