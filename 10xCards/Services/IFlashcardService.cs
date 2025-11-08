using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;

namespace _10xCards.Services;

/// <summary>
/// Service interface for flashcard operations
/// </summary>
public interface IFlashcardService {
	/// <summary>
	/// Retrieves a paginated list of flashcards for a specific user
	/// </summary>
	/// <param name="userId">The ID of the user whose flashcards to retrieve</param>
	/// <param name="query">Query parameters for filtering, sorting, and pagination</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the paginated list of flashcards</returns>
	Task<Result<FlashcardsListResponse>> ListFlashcardsAsync(
		Guid userId,
		ListFlashcardsQuery query,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves a specific flashcard by ID for a user
	/// </summary>
	/// <param name="userId">The ID of the user requesting the flashcard</param>
	/// <param name="flashcardId">The ID of the flashcard to retrieve</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the flashcard if found</returns>
	Task<Result<FlashcardResponse>> GetFlashcardAsync(
		Guid userId,
		long flashcardId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new flashcard manually for a specific user
	/// </summary>
	/// <param name="userId">The ID of the user creating the flashcard</param>
	/// <param name="request">The flashcard creation request</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the created flashcard</returns>
	Task<Result<FlashcardResponse>> CreateFlashcardAsync(
		Guid userId,
		CreateFlashcardRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates multiple flashcards from AI generation in a single batch
	/// </summary>
	/// <param name="userId">The ID of the user creating the flashcards</param>
	/// <param name="request">The batch flashcard creation request</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the created flashcards and count</returns>
	Task<Result<CreateFlashcardsBatchResponse>> CreateFlashcardsBatchAsync(
		Guid userId,
		CreateFlashcardsBatchRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing flashcard for a specific user
	/// </summary>
	/// <param name="userId">The ID of the user updating the flashcard</param>
	/// <param name="flashcardId">The ID of the flashcard to update</param>
	/// <param name="request">The flashcard update request</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the updated flashcard</returns>
	Task<Result<FlashcardResponse>> UpdateFlashcardAsync(
		Guid userId,
		long flashcardId,
		UpdateFlashcardRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a flashcard for a specific user
	/// </summary>
	/// <param name="userId">The ID of the user deleting the flashcard</param>
	/// <param name="flashcardId">The ID of the flashcard to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result<bool>> DeleteFlashcardAsync(
		Guid userId,
		long flashcardId,
		CancellationToken cancellationToken = default);
}

