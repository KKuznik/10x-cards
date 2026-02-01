namespace _10xCards.Database.Entities;

public sealed class Flashcard {
    public long Id { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? GenerationId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Generation? Generation { get; set; }
}

