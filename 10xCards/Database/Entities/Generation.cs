namespace _10xCards.Database.Entities;

public sealed class Generation {
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

	// Navigation properties
	public User User { get; set; } = null!;
	public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
}

