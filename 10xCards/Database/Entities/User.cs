using Microsoft.AspNetCore.Identity;

namespace _10xCards.Database.Entities;

public sealed class User : IdentityUser<Guid> {
	public DateTime? ConfirmedAt { get; set; }

	// Navigation properties
	public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
	public ICollection<Generation> Generations { get; set; } = new List<Generation>();
	public ICollection<GenerationErrorLog> GenerationErrorLogs { get; set; } = new List<GenerationErrorLog>();
}

