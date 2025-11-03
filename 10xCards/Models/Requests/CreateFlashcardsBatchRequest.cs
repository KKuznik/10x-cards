using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for creating multiple flashcards from AI generation
/// Maps to: POST /api/flashcards/batch
/// Based on: Generation and Flashcard entities
/// </summary>
public sealed class CreateFlashcardsBatchRequest {
	[Required(ErrorMessage = "Generation ID is required")]
	[Range(1, long.MaxValue, ErrorMessage = "Generation ID must be a positive number")]
	public long GenerationId { get; set; }

	[Required(ErrorMessage = "Flashcards array is required")]
	[MinLength(1, ErrorMessage = "At least one flashcard is required")]
	[MaxLength(50, ErrorMessage = "Cannot create more than 50 flashcards at once")]
	public List<BatchFlashcardItem> Flashcards { get; set; } = new();
}

