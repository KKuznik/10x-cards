using _10xCards.Database.Context;
using _10xCards.Database.Entities;
using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace _10xCards.Services;

/// <summary>
/// Service for generation operations
/// </summary>
public sealed class GenerationService : IGenerationService {
    private readonly ApplicationDbContext _context;
    private readonly IChatGptService _chatGptService;
    private readonly ILogger<GenerationService> _logger;

    public GenerationService(
        ApplicationDbContext context,
        ILogger<GenerationService> logger,
        IChatGptService chatGptService) {
        _context = context;
        _logger = logger;
        _chatGptService = chatGptService;
    }

    /// <inheritdoc />
    public async Task<Result<GenerationsListResponse>> ListGenerationsAsync(
        Guid userId,
        ListGenerationsQuery query,
        CancellationToken cancellationToken = default) {

        try {
            // Guard clause: validate userId
            if (userId == Guid.Empty) {
                _logger.LogWarning("ListGenerationsAsync called with empty userId");
                return Result<GenerationsListResponse>.Failure("Invalid user ID");
            }

            // Build base query with filtering by userId (Row-Level Security)
            var baseQuery = _context.Generations
                .AsNoTracking()
                .Where(g => g.UserId == userId);

            // Get total count for pagination
            var totalItems = await baseQuery.CountAsync(cancellationToken);

            // Apply sorting
            var sortedQuery = query.SortOrder.ToLower() == "asc"
                ? baseQuery.OrderBy(g => g.CreatedAt)
                : baseQuery.OrderByDescending(g => g.CreatedAt);

            // Apply pagination
            var items = await sortedQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs with calculated AcceptanceRate
            var data = items.Select(g => new GenerationListItemResponse {
                Id = g.Id,
                Model = g.Model,
                GeneratedCount = g.GeneratedCount,
                AcceptedUneditedCount = g.AcceptedUneditedCount,
                AcceptedEditedCount = g.AcceptedEditedCount,
                SourceTextLength = g.SourceTextLength,
                GenerationDuration = g.GenerationDuration,
                CreatedAt = g.CreatedAt,
                // Calculate acceptance rate: ((unedited + edited) / generated) * 100
                // Handle nullable values by treating null as 0
                AcceptanceRate = g.GeneratedCount > 0
                    ? ((g.AcceptedUneditedCount ?? 0) + (g.AcceptedEditedCount ?? 0)) / (double)g.GeneratedCount * 100
                    : 0.0
            }).ToList();

            // Get user statistics
            var stats = await baseQuery
                .GroupBy(g => g.UserId)
                .Select(group => new GenerationStatistics {
                    TotalGenerations = group.Count(),
                    TotalGenerated = group.Sum(g => g.GeneratedCount),
                    TotalAccepted = group.Sum(g => (g.AcceptedUneditedCount ?? 0) + (g.AcceptedEditedCount ?? 0))
                })
                .FirstOrDefaultAsync(cancellationToken);

            // If no generations exist, create default statistics
            if (stats == null) {
                stats = new GenerationStatistics {
                    TotalGenerations = 0,
                    TotalGenerated = 0,
                    TotalAccepted = 0,
                    OverallAcceptanceRate = 0.0
                };
            } else {
                // Calculate overall acceptance rate with division by zero protection
                stats.OverallAcceptanceRate = stats.TotalGenerated > 0
                    ? stats.TotalAccepted / (double)stats.TotalGenerated * 100
                    : 0.0;
            }

            // Build pagination metadata
            var pagination = new PaginationMetadata {
                CurrentPage = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)query.PageSize) : 0
            };

            // Build response
            var response = new GenerationsListResponse {
                Data = data,
                Pagination = pagination,
                Statistics = stats
            };

            _logger.LogInformation(
                "Successfully retrieved {Count} generations for user {UserId} (Page {Page}/{TotalPages})",
                data.Count, userId, query.Page, pagination.TotalPages);

            return Result<GenerationsListResponse>.Success(response);
        } catch (Exception ex) {
            _logger.LogError(ex,
                "Error retrieving generations for user {UserId} with query {@Query}",
                userId, query);
            return Result<GenerationsListResponse>.Failure(
                "An error occurred while retrieving generations");
        }
    }

    /// <inheritdoc />
    public async Task<Result<GenerationDetailResponse>> GetGenerationDetailsAsync(
        Guid userId,
        long generationId,
        CancellationToken cancellationToken = default) {

        try {
            // Guard clause: validate userId
            if (userId == Guid.Empty) {
                _logger.LogWarning("GetGenerationDetailsAsync called with empty userId");
                return Result<GenerationDetailResponse>.Failure("Invalid user ID");
            }

            // Guard clause: validate generationId
            if (generationId <= 0) {
                _logger.LogWarning("GetGenerationDetailsAsync called with invalid generationId: {GenerationId}", generationId);
                return Result<GenerationDetailResponse>.Failure("Generation not found");
            }

            // Query generation with flashcards (RLS enforced)
            var generation = await _context.Generations
                .AsNoTracking()
                .Include(g => g.Flashcards)
                .Where(g => g.Id == generationId && g.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            // Not found or doesn't belong to user (RLS)
            if (generation == null) {
                _logger.LogWarning(
                    "Generation {GenerationId} not found or doesn't belong to user {UserId}",
                    generationId, userId);
                return Result<GenerationDetailResponse>.Failure("Generation not found");
            }

            // Map to DTO
            var response = new GenerationDetailResponse {
                Id = generation.Id,
                UserId = generation.UserId,
                Model = generation.Model,
                GeneratedCount = generation.GeneratedCount,
                AcceptedUneditedCount = generation.AcceptedUneditedCount,
                AcceptedEditedCount = generation.AcceptedEditedCount,
                SourceTextHash = generation.SourceTextHash,
                SourceTextLength = generation.SourceTextLength,
                GenerationDuration = generation.GenerationDuration,
                CreatedAt = generation.CreatedAt,
                UpdatedAt = generation.UpdatedAt,
                Flashcards = generation.Flashcards
                    .Select(f => new FlashcardResponse {
                        Id = f.Id,
                        Front = f.Front,
                        Back = f.Back,
                        Source = f.Source,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt,
                        GenerationId = f.GenerationId
                    })
                    .OrderBy(f => f.CreatedAt) // Sorted by creation time
                    .ToList()
            };

            _logger.LogInformation(
                "Successfully retrieved generation {GenerationId} with {FlashcardCount} flashcards for user {UserId}",
                generationId, response.Flashcards.Count, userId);

            return Result<GenerationDetailResponse>.Success(response);
        } catch (Exception ex) {
            _logger.LogError(ex,
                "Error retrieving generation {GenerationId} for user {UserId}",
                generationId, userId);
            return Result<GenerationDetailResponse>.Failure(
                "An error occurred while retrieving generation details");
        }
    }

    /// <inheritdoc />
    public async Task<Result<GenerationResponse>> GenerateFlashcardsAsync(
        Guid userId,
        GenerateFlashcardsRequest request,
        CancellationToken cancellationToken = default) {

        // Guard clause: validate userId
        if (userId == Guid.Empty) {
            _logger.LogWarning("GenerateFlashcardsAsync called with empty userId");
            return Result<GenerationResponse>.Failure("Invalid user ID");
        }

        // Calculate SHA-256 hash of source text
        var sourceTextHash = CalculateSha256Hash(request.SourceText);
        var sourceTextLength = request.SourceText.Length;

        // Start stopwatch for duration measurement
        var stopwatch = Stopwatch.StartNew();

        List<ProposedFlashcardDto> flashcards;
        try {
            // Call AI service to generate flashcards
            _logger.LogInformation(
                "Generating flashcards for user {UserId} with model {Model}, text length {TextLength}",
                userId, request.Model, sourceTextLength);

            flashcards = await _chatGptService.GenerateFlashcardsAsync(
                request.SourceText,
                request.Model,
                cancellationToken);

            if (flashcards == null || flashcards.Count == 0) {
                _logger.LogWarning("AI service returned no flashcards for user {UserId}", userId);
                return Result<GenerationResponse>.Failure("AI service did not generate any flashcards");
            }
        } catch (HttpRequestException ex) {
            stopwatch.Stop();
            _logger.LogError(ex,
                "HTTP error during AI generation for user {UserId}, model {Model}",
                userId, request.Model);

            // Log error to database
            await LogGenerationErrorAsync(
                userId,
                request.Model,
                sourceTextHash,
                sourceTextLength,
                "AI_API_ERROR",
                $"HTTP error: {ex.Message}",
                cancellationToken);

            // Return appropriate error based on exception
            if (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)) {
                return Result<GenerationResponse>.Failure("AI service request timed out. Please try again.");
            }

            return Result<GenerationResponse>.Failure("Failed to generate flashcards. Please try again later.");
        } catch (InvalidOperationException ex) {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Invalid operation during AI generation for user {UserId}, model {Model}",
                userId, request.Model);

            // Log error to database
            await LogGenerationErrorAsync(
                userId,
                request.Model,
                sourceTextHash,
                sourceTextLength,
                "AI_INVALID_RESPONSE",
                $"Invalid response: {ex.Message}",
                cancellationToken);

            return Result<GenerationResponse>.Failure("Failed to parse AI response. Please try again.");
        } catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unexpected error during AI generation for user {UserId}, model {Model}",
                userId, request.Model);

            // Log error to database
            await LogGenerationErrorAsync(
                userId,
                request.Model,
                sourceTextHash,
                sourceTextLength,
                "AI_GENERATION_ERROR",
                $"Unexpected error: {ex.Message}",
                cancellationToken);

            return Result<GenerationResponse>.Failure("An unexpected error occurred. Please try again later.");
        }

        // Stop stopwatch and calculate duration in milliseconds
        stopwatch.Stop();
        var generationDuration = (int)stopwatch.ElapsedMilliseconds;

        // Create Generation entity
        var generation = new Generation {
            UserId = userId,
            Model = request.Model,
            GeneratedCount = flashcards.Count,
            AcceptedUneditedCount = null, // Not accepted yet
            AcceptedEditedCount = null,   // Not accepted yet
            SourceTextHash = sourceTextHash,
            SourceTextLength = sourceTextLength,
            GenerationDuration = generationDuration,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try {
            // Save generation to database
            await _context.Generations.AddAsync(generation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully generated {Count} flashcards for user {UserId} in {Duration}ms (Generation ID: {GenerationId})",
                flashcards.Count, userId, generationDuration, generation.Id);

            // Map to response DTO
            var response = new GenerationResponse {
                Id = generation.Id,
                UserId = generation.UserId,
                Model = generation.Model,
                GeneratedCount = generation.GeneratedCount,
                AcceptedUneditedCount = generation.AcceptedUneditedCount,
                AcceptedEditedCount = generation.AcceptedEditedCount,
                SourceTextHash = generation.SourceTextHash,
                SourceTextLength = generation.SourceTextLength,
                GenerationDuration = generation.GenerationDuration,
                CreatedAt = generation.CreatedAt,
                UpdatedAt = generation.UpdatedAt,
                Flashcards = flashcards
            };

            return Result<GenerationResponse>.Success(response);
        } catch (Exception ex) {
            _logger.LogError(ex,
                "Database error while saving generation for user {UserId}",
                userId);
            return Result<GenerationResponse>.Failure(
                "An error occurred while saving generation data");
        }
    }

    /// <summary>
    /// Calculates SHA-256 hash of input string
    /// </summary>
    private static string CalculateSha256Hash(string input) {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Logs generation error to database
    /// </summary>
    private async Task LogGenerationErrorAsync(
        Guid userId,
        string model,
        string sourceTextHash,
        int sourceTextLength,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken) {

        try {
            var errorLog = new GenerationErrorLog {
                UserId = userId,
                Model = model,
                SourceTextHash = sourceTextHash,
                SourceTextLength = sourceTextLength,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _context.GenerationErrorLogs.AddAsync(errorLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Logged generation error {ErrorCode} for user {UserId}",
                errorCode, userId);
        } catch (Exception ex) {
            // Don't throw - error logging failure shouldn't prevent response
            _logger.LogError(ex,
                "Failed to log generation error to database for user {UserId}",
                userId);
        }
    }
}

