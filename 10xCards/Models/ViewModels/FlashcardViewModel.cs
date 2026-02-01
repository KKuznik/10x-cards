namespace _10xCards.Models.ViewModels;

/// <summary>
/// View model for managing flashcard state in the Generate view
/// Tracks original AI-generated content, current edits, and user decisions
/// </summary>
public sealed class FlashcardViewModel {
    /// <summary>
    /// Original front text from AI generation (immutable)
    /// </summary>
    public string OriginalFront { get; set; } = string.Empty;

    /// <summary>
    /// Original back text from AI generation (immutable)
    /// </summary>
    public string OriginalBack { get; set; } = string.Empty;

    /// <summary>
    /// Current front text (may be edited by user)
    /// </summary>
    public string Front { get; set; } = string.Empty;

    /// <summary>
    /// Current back text (may be edited by user)
    /// </summary>
    public string Back { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the flashcard (Pending/Accepted/Rejected)
    /// </summary>
    public FlashcardStatus Status { get; set; } = FlashcardStatus.Pending;

    /// <summary>
    /// Whether the user has edited this flashcard
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// Whether the flashcard is currently in edit mode
    /// </summary>
    public bool IsInEditMode { get; set; }

    /// <summary>
    /// Source type for API submission: "ai-full" if unedited, "ai-edited" if modified
    /// </summary>
    public string Source => IsEdited ? "ai-edited" : "ai-full";
}

/// <summary>
/// Status of a flashcard in the generation review process
/// </summary>
public enum FlashcardStatus {
    /// <summary>
    /// Default state - awaiting user decision
    /// </summary>
    Pending,

    /// <summary>
    /// User has accepted this flashcard for saving
    /// </summary>
    Accepted,

    /// <summary>
    /// User has rejected this flashcard
    /// </summary>
    Rejected
}

/// <summary>
/// Overall state of the generation process
/// </summary>
public enum GenerationState {
    /// <summary>
    /// Initial state - ready to generate
    /// </summary>
    Idle,

    /// <summary>
    /// Currently generating flashcards via API
    /// </summary>
    Generating,

    /// <summary>
    /// Flashcards have been generated and are being reviewed
    /// </summary>
    Generated,

    /// <summary>
    /// Currently saving accepted flashcards to database
    /// </summary>
    Saving,

    /// <summary>
    /// Flashcards have been successfully saved
    /// </summary>
    Saved,

    /// <summary>
    /// An error occurred during generation or saving
    /// </summary>
    Error
}

