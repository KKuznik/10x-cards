using _10xCards.Models.Responses;

namespace _10xCards.Services;

/// <summary>
/// Service interface for OpenRouter AI integration
/// </summary>
public interface IOpenRouterService {
	/// <summary>
	/// Generates flashcard suggestions from source text using AI
	/// </summary>
	/// <param name="sourceText">The text to generate flashcards from</param>
	/// <param name="model">The OpenRouter model identifier (e.g., "openai/gpt-4o-mini")</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of proposed flashcards</returns>
	/// <exception cref="HttpRequestException">Thrown when API request fails</exception>
	/// <exception cref="InvalidOperationException">Thrown when response cannot be parsed</exception>
	Task<List<ProposedFlashcardDto>> GenerateFlashcardsAsync(
		string sourceText,
		string model,
		CancellationToken cancellationToken = default);
}

