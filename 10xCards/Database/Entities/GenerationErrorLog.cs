namespace _10xCards.Database.Entities;

public sealed class GenerationErrorLog {
	public long Id { get; set; }
	public Guid UserId { get; set; }
	public string Model { get; set; } = string.Empty;
	public string SourceTextHash { get; set; } = string.Empty;
	public int SourceTextLength { get; set; }
	public string ErrorCode { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }

	// Navigation property
	public User User { get; set; } = null!;
}

