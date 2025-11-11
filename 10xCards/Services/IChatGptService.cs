using _10xCards.Models.Responses;

namespace _10xCards.Services;

/// <summary>
/// Service interface for ChatGPT (OpenAI) integration
/// </summary>
public interface IChatGptService {
	/// <summary>
	/// Generates flashcard suggestions from source text using ChatGPT
	/// </summary>
	/// <param name="sourceText">The text to generate flashcards from</param>
	/// <param name="model">The OpenAI model identifier (e.g., "gpt-4o", "gpt-3.5-turbo")</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of proposed flashcards</returns>
	/// <exception cref="HttpRequestException">Thrown when API request fails</exception>
	/// <exception cref="InvalidOperationException">Thrown when response cannot be parsed</exception>
	Task<List<ProposedFlashcardDto>> GenerateFlashcardsAsync(
		string sourceText,
		string model,
		CancellationToken cancellationToken = default);
}

