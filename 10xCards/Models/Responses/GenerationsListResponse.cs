using _10xCards.Models.Common;

namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for paginated generations list with statistics
/// Maps to: GET /api/generations
/// </summary>
public sealed class GenerationsListResponse {
    public List<GenerationListItemResponse> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
    public GenerationStatistics Statistics { get; set; } = new();
}

