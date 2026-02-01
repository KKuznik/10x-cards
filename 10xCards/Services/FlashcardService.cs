using _10xCards.Database.Context;
using _10xCards.Database.Entities;
using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Services;

/// <summary>
/// Service for flashcard operations including listing, filtering, and searching
/// </summary>
public sealed class FlashcardService : IFlashcardService {
	private readonly ApplicationDbContext _context;
	private readonly ILogger<FlashcardService> _logger;

	public FlashcardService(
		ApplicationDbContext context,
		ILogger<FlashcardService> logger) {
		_context = context;
		_logger = logger;
	}

	public async Task<Result<FlashcardsListResponse>> ListFlashcardsAsync(
		Guid userId,
		ListFlashcardsQuery query,
		CancellationToken cancellationToken = default) {

		try {
			// Guard clause: validate userId
			if (userId == Guid.Empty) {
				_logger.LogWarning("ListFlashcardsAsync called with empty userId");
				return Result<FlashcardsListResponse>.Failure("Invalid user ID");
			}

			// Guard clause: validate query
			if (query is null) {
				_logger.LogWarning("ListFlashcardsAsync called with null query. UserId: {UserId}", userId);
				return Result<FlashcardsListResponse>.Failure("Query parameters are required");
			}

			// Build base query with user filter (row-level security)
			var baseQuery = _context.Flashcards
				.AsNoTracking()
				.Where(f => f.UserId == userId);

			// Optional source filter
			if (!string.IsNullOrEmpty(query.Source)) {
				baseQuery = baseQuery.Where(f => f.Source == query.Source);
			}

			// Optional search filter (case-insensitive search in front and back)
			if (!string.IsNullOrEmpty(query.Search)) {
				var searchLower = query.Search.ToLower();
				baseQuery = baseQuery.Where(f =>
					f.Front.ToLower().Contains(searchLower) ||
					f.Back.ToLower().Contains(searchLower));
			}

			// Get total count for pagination metadata
			var totalItems = await baseQuery.CountAsync(cancellationToken);

			// Apply sorting
			var sortedQuery = query.SortBy switch {
				"front" => query.SortOrder == "asc"
					? baseQuery.OrderBy(f => f.Front)
					: baseQuery.OrderByDescending(f => f.Front),
				"updatedAt" => query.SortOrder == "asc"
					? baseQuery.OrderBy(f => f.UpdatedAt)
					: baseQuery.OrderByDescending(f => f.UpdatedAt),
				_ => query.SortOrder == "asc"
					? baseQuery.OrderBy(f => f.CreatedAt)
					: baseQuery.OrderByDescending(f => f.CreatedAt)
			};

			// Apply pagination
			var flashcards = await sortedQuery
				.Skip((query.Page - 1) * query.PageSize)
				.Take(query.PageSize)
				.ToListAsync(cancellationToken);

			// Map entities to response DTOs
			var flashcardResponses = flashcards.Select(f => new FlashcardResponse {
				Id = f.Id,
				Front = f.Front,
				Back = f.Back,
				Source = f.Source,
				CreatedAt = f.CreatedAt,
				UpdatedAt = f.UpdatedAt,
				GenerationId = f.GenerationId
			}).ToList();

			// Calculate pagination metadata
			var totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);

			var response = new FlashcardsListResponse {
				Data = flashcardResponses,
				Pagination = new PaginationMetadata {
					CurrentPage = query.Page,
					PageSize = query.PageSize,
					TotalPages = totalPages,
					TotalItems = totalItems
				}
			};

			_logger.LogInformation(
				"Successfully retrieved flashcards. UserId: {UserId}, Page: {Page}, PageSize: {PageSize}, TotalItems: {TotalItems}",
				userId, query.Page, query.PageSize, totalItems);

			// Happy path: return success response
			return Result<FlashcardsListResponse>.Success(response);
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Failed to list flashcards. UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
				userId, query.Page, query.PageSize);

			return Result<FlashcardsListResponse>.Failure("An error occurred while retrieving flashcards");
		}
	}

	public async Task<Result<FlashcardResponse>> GetFlashcardAsync(
		Guid userId,
		long flashcardId,
		CancellationToken cancellationToken = default) {

		try {
			// Guard clause: validate userId
			if (userId == Guid.Empty) {
				_logger.LogWarning("GetFlashcardAsync called with empty userId");
				return Result<FlashcardResponse>.Failure("Invalid user ID");
			}

			// Guard clause: validate flashcardId
			if (flashcardId <= 0) {
				_logger.LogWarning("GetFlashcardAsync called with invalid flashcardId: {FlashcardId}", flashcardId);
				return Result<FlashcardResponse>.Failure("Flashcard not found");
			}

			// Query database with RLS (row-level security)
			var flashcard = await _context.Flashcards
				.AsNoTracking()
				.Where(f => f.Id == flashcardId && f.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);

			// Guard clause: verify flashcard exists and belongs to user
			if (flashcard is null) {
				_logger.LogWarning(
					"Flashcard not found or doesn't belong to user. UserId: {UserId}, FlashcardId: {FlashcardId}",
					userId, flashcardId);
				return Result<FlashcardResponse>.Failure("Flashcard not found");
			}

			// Map to response DTO
			var response = new FlashcardResponse {
				Id = flashcard.Id,
				Front = flashcard.Front,
				Back = flashcard.Back,
				Source = flashcard.Source,
				CreatedAt = flashcard.CreatedAt,
				UpdatedAt = flashcard.UpdatedAt,
				GenerationId = flashcard.GenerationId
			};

			_logger.LogInformation(
				"Successfully retrieved flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);

			// Happy path: return success
			return Result<FlashcardResponse>.Success(response);
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Error retrieving flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);
			return Result<FlashcardResponse>.Failure(
				"An error occurred while retrieving the flashcard");
		}
	}

	public async Task<Result<FlashcardResponse>> CreateFlashcardAsync(
		Guid userId,
		CreateFlashcardRequest request,
		CancellationToken cancellationToken = default) {

		try {
			// Guard clause: validate userId
			if (userId == Guid.Empty) {
				_logger.LogWarning("CreateFlashcardAsync called with empty userId");
				return Result<FlashcardResponse>.Failure("Invalid user ID");
			}

			// Guard clause: validate request
			if (request is null) {
				_logger.LogWarning("CreateFlashcardAsync called with null request. UserId: {UserId}", userId);
				return Result<FlashcardResponse>.Failure("Request is required");
			}

			// Create new flashcard entity
			var flashcard = new Flashcard {
				Front = request.Front.Trim(),
				Back = request.Back.Trim(),
				Source = "manual",
				GenerationId = null,
				UserId = userId,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			// Add to context and save
			_context.Flashcards.Add(flashcard);
			await _context.SaveChangesAsync(cancellationToken);

			// Map to response DTO
			var response = new FlashcardResponse {
				Id = flashcard.Id,
				Front = flashcard.Front,
				Back = flashcard.Back,
				Source = flashcard.Source,
				CreatedAt = flashcard.CreatedAt,
				UpdatedAt = flashcard.UpdatedAt,
				GenerationId = flashcard.GenerationId
			};

			_logger.LogInformation(
				"Successfully created flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcard.Id);

			// Happy path: return success
			return Result<FlashcardResponse>.Success(response);
		} catch (DbUpdateException ex) {
			_logger.LogError(ex,
				"Database error while creating flashcard. UserId: {UserId}", userId);
			return Result<FlashcardResponse>.Failure(
				"An error occurred while saving the flashcard");
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Unexpected error while creating flashcard. UserId: {UserId}", userId);
			return Result<FlashcardResponse>.Failure(
				"An unexpected error occurred");
		}
	}

	public async Task<Result<CreateFlashcardsBatchResponse>> CreateFlashcardsBatchAsync(
		Guid userId,
		CreateFlashcardsBatchRequest request,
		CancellationToken cancellationToken = default) {

		try {
			// Guard clause: validate userId
			if (userId == Guid.Empty) {
				_logger.LogWarning("CreateFlashcardsBatchAsync called with empty userId");
				return Result<CreateFlashcardsBatchResponse>.Failure("Invalid user ID");
			}

			// Guard clause: validate request
			if (request is null) {
				_logger.LogWarning("CreateFlashcardsBatchAsync called with null request. UserId: {UserId}", userId);
				return Result<CreateFlashcardsBatchResponse>.Failure("Request is required");
			}

			// Guard clause: validate flashcards list is not empty
			if (request.Flashcards is null || request.Flashcards.Count == 0) {
				_logger.LogWarning("CreateFlashcardsBatchAsync called with empty flashcards list. UserId: {UserId}", userId);
				return Result<CreateFlashcardsBatchResponse>.Failure("At least one flashcard is required");
			}

			// Query generation with tracking (will be updated)
			var generation = await _context.Generations
				.Where(g => g.Id == request.GenerationId && g.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);

			// Guard clause: verify generation exists and belongs to user
			if (generation is null) {
				_logger.LogWarning(
					"CreateFlashcardsBatchAsync: Generation not found or does not belong to user. UserId: {UserId}, GenerationId: {GenerationId}",
					userId, request.GenerationId);
				return Result<CreateFlashcardsBatchResponse>.Failure("Generation not found or does not belong to user");
			}

			// Calculate acceptance counts
			var uneditedCount = request.Flashcards.Count(f => f.Source == "ai-full");
			var editedCount = request.Flashcards.Count(f => f.Source == "ai-edited");

			// Create flashcard entities
			var flashcards = new List<Flashcard>(request.Flashcards.Count);
			var currentTime = DateTime.UtcNow;

			foreach (var item in request.Flashcards) {
				var flashcard = new Flashcard {
					Front = item.Front.Trim(),
					Back = item.Back.Trim(),
					Source = item.Source,
					GenerationId = request.GenerationId,
					UserId = userId,
					CreatedAt = currentTime,
					UpdatedAt = currentTime
				};
				flashcards.Add(flashcard);
				_context.Flashcards.Add(flashcard);
			}

			// Update generation statistics
			generation.AcceptedUneditedCount = (generation.AcceptedUneditedCount ?? 0) + uneditedCount;
			generation.AcceptedEditedCount = (generation.AcceptedEditedCount ?? 0) + editedCount;
			generation.UpdatedAt = currentTime;

			// Save all changes in a single transaction
			await _context.SaveChangesAsync(cancellationToken);

			// Map entities to response DTOs
			var flashcardResponses = flashcards.Select(f => new FlashcardResponse {
				Id = f.Id,
				Front = f.Front,
				Back = f.Back,
				Source = f.Source,
				CreatedAt = f.CreatedAt,
				UpdatedAt = f.UpdatedAt,
				GenerationId = f.GenerationId
			}).ToList();

			var response = new CreateFlashcardsBatchResponse {
				Created = flashcards.Count,
				Flashcards = flashcardResponses
			};

			_logger.LogInformation(
				"Successfully created batch of flashcards. UserId: {UserId}, GenerationId: {GenerationId}, Count: {Count}, Unedited: {Unedited}, Edited: {Edited}",
				userId, request.GenerationId, flashcards.Count, uneditedCount, editedCount);

			// Happy path: return success
			return Result<CreateFlashcardsBatchResponse>.Success(response);
		} catch (DbUpdateException ex) {
			_logger.LogError(ex,
				"Database error while creating batch flashcards. UserId: {UserId}, GenerationId: {GenerationId}",
				userId, request?.GenerationId);
			return Result<CreateFlashcardsBatchResponse>.Failure(
				"An error occurred while saving the flashcards");
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Unexpected error while creating batch flashcards. UserId: {UserId}, GenerationId: {GenerationId}",
				userId, request?.GenerationId);
			return Result<CreateFlashcardsBatchResponse>.Failure(
				"An unexpected error occurred while creating flashcards");
		}
	}

	public async Task<Result<FlashcardResponse>> UpdateFlashcardAsync(
		Guid userId,
		long flashcardId,
		UpdateFlashcardRequest request,
		CancellationToken cancellationToken = default) {

		try {
			// Guard clause: validate userId
			if (userId == Guid.Empty) {
				_logger.LogWarning("UpdateFlashcardAsync called with empty userId");
				return Result<FlashcardResponse>.Failure("Invalid user ID");
			}

			// Guard clause: validate request
			if (request is null) {
				_logger.LogWarning("UpdateFlashcardAsync called with null request. UserId: {UserId}", userId);
				return Result<FlashcardResponse>.Failure("Request is required");
			}

			// Query flashcard with tracking (will be updated)
			var flashcard = await _context.Flashcards
				.Where(f => f.Id == flashcardId && f.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);

			// Guard clause: verify flashcard exists and belongs to user
			if (flashcard is null) {
				_logger.LogWarning(
					"UpdateFlashcardAsync: Flashcard not found or does not belong to user. UserId: {UserId}, FlashcardId: {FlashcardId}",
					userId, flashcardId);
				return Result<FlashcardResponse>.Failure("Flashcard not found or does not belong to user");
			}

			// Trim input strings
			var trimmedFront = request.Front.Trim();
			var trimmedBack = request.Back.Trim();

			// Update flashcard properties
			flashcard.Front = trimmedFront;
			flashcard.Back = trimmedBack;
			flashcard.UpdatedAt = DateTime.UtcNow;

			// Apply source logic: if originally 'ai-full', change to 'ai-edited'
			if (flashcard.Source == "ai-full") {
				flashcard.Source = "ai-edited";
			}

			// GenerationId is preserved (no changes)

			// Save changes
			await _context.SaveChangesAsync(cancellationToken);

			// Map to response DTO
			var response = new FlashcardResponse {
				Id = flashcard.Id,
				Front = flashcard.Front,
				Back = flashcard.Back,
				Source = flashcard.Source,
				CreatedAt = flashcard.CreatedAt,
				UpdatedAt = flashcard.UpdatedAt,
				GenerationId = flashcard.GenerationId
			};

			_logger.LogInformation(
				"Successfully updated flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}, SourceChanged: {SourceChanged}",
				userId, flashcard.Id, flashcard.Source == "ai-edited");

			// Happy path: return success
			return Result<FlashcardResponse>.Success(response);
		} catch (DbUpdateException ex) {
			_logger.LogError(ex,
				"Database error while updating flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);
			return Result<FlashcardResponse>.Failure(
				"An error occurred while updating the flashcard");
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Unexpected error while updating flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);
			return Result<FlashcardResponse>.Failure(
				"An unexpected error occurred");
		}
	}

	public async Task<Result<bool>> DeleteFlashcardAsync(
		Guid userId,
		long flashcardId,
		CancellationToken cancellationToken = default) {

		try {
			// Guard clause: validate userId
			if (userId == Guid.Empty) {
				_logger.LogWarning("DeleteFlashcardAsync called with empty userId");
				return Result<bool>.Failure("Invalid user ID");
			}

			// Query flashcard with tracking (will be deleted)
			var flashcard = await _context.Flashcards
				.Where(f => f.Id == flashcardId && f.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);

			// Guard clause: verify flashcard exists and belongs to user
			if (flashcard is null) {
				_logger.LogWarning(
					"DeleteFlashcardAsync: Flashcard not found or does not belong to user. UserId: {UserId}, FlashcardId: {FlashcardId}",
					userId, flashcardId);
				return Result<bool>.Failure("Flashcard not found or does not belong to user");
			}

			// Remove flashcard from context
			_context.Flashcards.Remove(flashcard);

			// Save changes to database
			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation(
				"Successfully deleted flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);

			// Happy path: return success
			return Result<bool>.Success(true);
		} catch (DbUpdateException ex) {
			_logger.LogError(ex,
				"Database error while deleting flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);
			return Result<bool>.Failure(
				"An error occurred while deleting the flashcard");
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Unexpected error while deleting flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
				userId, flashcardId);
			return Result<bool>.Failure(
				"An unexpected error occurred");
		}
	}
}

