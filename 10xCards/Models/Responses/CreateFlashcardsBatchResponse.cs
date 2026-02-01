namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for batch flashcard creation
/// Maps to: POST /api/flashcards/batch
/// </summary>
public sealed class CreateFlashcardsBatchResponse {
    public int Created { get; set; }
    public List<FlashcardResponse> Flashcards { get; set; } = new();
}

