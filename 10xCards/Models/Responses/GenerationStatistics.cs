namespace _10xCards.Models.Responses;

/// <summary>
/// Overall statistics for user's generations
/// Used in: GenerationsListResponse
/// </summary>
public sealed class GenerationStatistics {
    public int TotalGenerations { get; set; }
    public int TotalGenerated { get; set; }
    public int TotalAccepted { get; set; }

    /// <summary>
    /// Overall acceptance rate: (TotalAccepted / TotalGenerated) * 100
    /// Calculated in service layer
    /// </summary>
    public double OverallAcceptanceRate { get; set; }
}

