namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for generation list item with computed acceptance rate
/// Used in: GenerationsListResponse
/// Based on: Generation entity
/// </summary>
public sealed class GenerationListItemResponse {
	public long Id { get; set; }
	public string Model { get; set; } = string.Empty;
	public int GeneratedCount { get; set; }
	public int? AcceptedUneditedCount { get; set; }
	public int? AcceptedEditedCount { get; set; }
	public int SourceTextLength { get; set; }
	public int GenerationDuration { get; set; }
	public DateTime CreatedAt { get; set; }
	
	/// <summary>
	/// Computed acceptance rate: ((AcceptedUneditedCount + AcceptedEditedCount) / GeneratedCount) * 100
	/// Calculated in service layer
	/// </summary>
	public double AcceptanceRate { get; set; }
}

