using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;

namespace _10xCards.Services;

/// <summary>
/// Service interface for generation operations
/// </summary>
public interface IGenerationService {
	/// <summary>
	/// Retrieves a paginated list of generations for a specific user with statistics
	/// </summary>
	/// <param name="userId">The ID of the user whose generations to retrieve</param>
	/// <param name="query">Query parameters for sorting and pagination</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the paginated list of generations with statistics</returns>
	Task<Result<GenerationsListResponse>> ListGenerationsAsync(
		Guid userId,
		ListGenerationsQuery query,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves detailed information about a specific generation with flashcards
	/// </summary>
	/// <param name="userId">The ID of the authenticated user</param>
	/// <param name="generationId">The ID of the generation to retrieve</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing generation details with flashcards or error</returns>
	Task<Result<GenerationDetailResponse>> GetGenerationDetailsAsync(
		Guid userId,
		long generationId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates flashcard suggestions using AI
	/// </summary>
	/// <param name="userId">The ID of the authenticated user</param>
	/// <param name="request">The generation request containing source text and model</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing generation metadata and proposed flashcards</returns>
	Task<Result<GenerationResponse>> GenerateFlashcardsAsync(
		Guid userId,
		GenerateFlashcardsRequest request,
		CancellationToken cancellationToken = default);
}

