using _10xCards.Models.Requests;
using _10xCards.Models.Responses;
using _10xCards.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _10xCards.Endpoints;

/// <summary>
/// Extension methods for mapping flashcard endpoints
/// </summary>
public static class FlashcardEndpoints {
	/// <summary>
	/// Maps all flashcard-related endpoints
	/// </summary>
	public static WebApplication MapFlashcardEndpoints(this WebApplication app) {
		var group = app.MapGroup("/api/flashcards")
			.WithTags("Flashcards")
			.RequireAuthorization();

		// GET /api/flashcards
		group.MapGet("", async (
			[AsParameters] ListFlashcardsQuery query,
			IFlashcardService flashcardService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await flashcardService.ListFlashcardsAsync(userId, query, cancellationToken);

			if (!result.IsSuccess) {
				return Results.BadRequest(new { message = result.ErrorMessage });
			}

			return Results.Ok(result.Value);
		})
		.WithName("ListFlashcards")
		.WithSummary("Get paginated list of user's flashcards")
		.WithDescription("Retrieves flashcards with optional filtering, sorting, and search")
		.Produces<FlashcardsListResponse>(StatusCodes.Status200OK)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized);

		// GET /api/flashcards/{id}
		group.MapGet("/{id:long}", async (
			long id,
			IFlashcardService flashcardService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await flashcardService.GetFlashcardAsync(userId, id, cancellationToken);

			if (!result.IsSuccess) {
				// Check for "not found" error for 404 response
				if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true) {
					return Results.NotFound(new { message = result.ErrorMessage });
				}
				return Results.Problem(
					detail: result.ErrorMessage,
					statusCode: StatusCodes.Status500InternalServerError);
			}

			return Results.Ok(result.Value);
		})
		.WithName("GetFlashcard")
		.WithSummary("Get a specific flashcard by ID")
		.WithDescription("Retrieves detailed information about a single flashcard owned by the authenticated user")
		.Produces<FlashcardResponse>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status401Unauthorized)
		.Produces(StatusCodes.Status404NotFound)
		.ProducesProblem(StatusCodes.Status500InternalServerError);

		// POST /api/flashcards
		group.MapPost("", async (
			[FromBody] CreateFlashcardRequest request,
			IFlashcardService flashcardService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

		var result = await flashcardService.CreateFlashcardAsync(userId, request, cancellationToken);

		if (!result.IsSuccess) {
			return Results.BadRequest(new { message = result.ErrorMessage });
		}

		return Results.Created($"/api/flashcards/{result.Value!.Id}", result.Value);
		})
		.WithName("CreateFlashcard")
		.WithSummary("Create a new flashcard manually")
		.WithDescription("Creates a single flashcard with front and back content")
		.Produces<FlashcardResponse>(StatusCodes.Status201Created)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized);

		// POST /api/flashcards/batch
		group.MapPost("/batch", async (
			[FromBody] CreateFlashcardsBatchRequest request,
			IFlashcardService flashcardService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await flashcardService.CreateFlashcardsBatchAsync(userId, request, cancellationToken);

			if (!result.IsSuccess) {
				// Check if it's a "not found" error for 404 response
				if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true) {
					return Results.NotFound(new { message = result.ErrorMessage });
				}
				return Results.BadRequest(new { message = result.ErrorMessage });
			}

			return Results.Created($"/api/flashcards/batch", result.Value);
		})
		.WithName("CreateFlashcardsBatch")
		.WithSummary("Create multiple flashcards from AI generation")
		.WithDescription("Accepts 1-50 flashcards from generation and updates statistics")
		.Produces<CreateFlashcardsBatchResponse>(StatusCodes.Status201Created)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized)
		.Produces(StatusCodes.Status404NotFound);

		// PUT /api/flashcards/{id}
		group.MapPut("/{id:long}", async (
			long id,
			[FromBody] UpdateFlashcardRequest request,
			IFlashcardService flashcardService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await flashcardService.UpdateFlashcardAsync(userId, id, request, cancellationToken);

			if (!result.IsSuccess) {
				// Check for "not found" error for 404 response
				if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true) {
					return Results.NotFound(new { message = result.ErrorMessage });
				}
				return Results.BadRequest(new { message = result.ErrorMessage });
			}

			return Results.Ok(result.Value);
		})
		.WithName("UpdateFlashcard")
		.WithSummary("Update an existing flashcard")
		.WithDescription("Updates the front and back content of a flashcard. AI-generated flashcards are marked as edited.")
		.Produces<FlashcardResponse>(StatusCodes.Status200OK)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized)
		.Produces(StatusCodes.Status404NotFound);

		// DELETE /api/flashcards/{id}
		group.MapDelete("/{id:long}", async (
			long id,
			IFlashcardService flashcardService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await flashcardService.DeleteFlashcardAsync(userId, id, cancellationToken);

			if (!result.IsSuccess) {
				// Check for "not found" error for 404 response
				if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true) {
					return Results.NotFound(new { message = result.ErrorMessage });
				}
				return Results.BadRequest(new { message = result.ErrorMessage });
			}

			return Results.NoContent();
		})
		.WithName("DeleteFlashcard")
		.WithSummary("Delete a flashcard")
		.WithDescription("Deletes a flashcard owned by the authenticated user")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status401Unauthorized)
		.Produces(StatusCodes.Status404NotFound)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest);

		return app;
	}
}

