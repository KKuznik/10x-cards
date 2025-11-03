using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for updating an existing flashcard
/// Maps to: PUT /api/flashcards/{id}
/// Based on: Flashcard entity
/// </summary>
public sealed class UpdateFlashcardRequest {
	[Required(ErrorMessage = "Front is required")]
	[MaxLength(200, ErrorMessage = "Front must not exceed 200 characters")]
	[MinLength(1, ErrorMessage = "Front cannot be empty")]
	public string Front { get; set; } = string.Empty;

	[Required(ErrorMessage = "Back is required")]
	[MaxLength(500, ErrorMessage = "Back must not exceed 500 characters")]
	[MinLength(1, ErrorMessage = "Back cannot be empty")]
	public string Back { get; set; } = string.Empty;
}

