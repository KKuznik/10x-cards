using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for AI flashcard generation
/// Maps to: POST /api/generations
/// Based on: Generation entity
/// </summary>
public sealed class GenerateFlashcardsRequest {
    [Required(ErrorMessage = "Source text is required")]
    [MinLength(1000, ErrorMessage = "Source text must be at least 1000 characters")]
    [MaxLength(10000, ErrorMessage = "Source text must not exceed 10000 characters")]
    public string SourceText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required")]
    [MaxLength(100, ErrorMessage = "Model identifier must not exceed 100 characters")]
    public string Model { get; set; } = string.Empty;
}

