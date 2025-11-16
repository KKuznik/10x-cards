using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for creating a single flashcard manually
/// Maps to: POST /api/flashcards
/// Based on: Flashcard entity
/// </summary>
public sealed class CreateFlashcardRequest {
	[Required(ErrorMessage = "Front is required")]
	[MaxLength(200, ErrorMessage = "Front must not exceed 200 characters")]
	[MinLength(1, ErrorMessage = "Front cannot be empty")]
	public string Front { get; set; } = string.Empty;

	[Required(ErrorMessage = "Back is required")]
	[MaxLength(500, ErrorMessage = "Back must not exceed 500 characters")]
	[MinLength(1, ErrorMessage = "Back cannot be empty")]
	public string Back { get; set; } = string.Empty;

	[MaxLength(500, ErrorMessage = "Source must not exceed 500 characters")]
	public string Source { get; set; } = string.Empty;
}

