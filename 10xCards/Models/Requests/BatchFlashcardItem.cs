using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Individual flashcard item in batch creation request
/// Nested in: CreateFlashcardsBatchRequest
/// Based on: Flashcard entity
/// </summary>
public sealed class BatchFlashcardItem {
	[Required(ErrorMessage = "Front is required")]
	[MaxLength(200, ErrorMessage = "Front must not exceed 200 characters")]
	[MinLength(1, ErrorMessage = "Front cannot be empty")]
	public string Front { get; set; } = string.Empty;

	[Required(ErrorMessage = "Back is required")]
	[MaxLength(500, ErrorMessage = "Back must not exceed 500 characters")]
	[MinLength(1, ErrorMessage = "Back cannot be empty")]
	public string Back { get; set; } = string.Empty;

	[Required(ErrorMessage = "Source is required")]
	[RegularExpression("^(ai-full|ai-edited)$", ErrorMessage = "Source must be 'ai-full' or 'ai-edited'")]
	public string Source { get; set; } = string.Empty;
}

