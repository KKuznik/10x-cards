using _10xCards.Models.Requests;
using _10xCards.Models.Responses;
using _10xCards.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _10xCards.Endpoints;

/// <summary>
/// Extension methods for mapping generation endpoints
/// </summary>
public static class GenerationEndpoints {
	/// <summary>
	/// Maps all generation-related endpoints
	/// </summary>
	public static WebApplication MapGenerationEndpoints(this WebApplication app) {
		var group = app.MapGroup("/api/generations")
			.WithTags("Generations")
			.RequireAuthorization();

		// GET /api/generations
		group.MapGet("", async (
			[AsParameters] ListGenerationsQuery query,
			IGenerationService generationService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await generationService.ListGenerationsAsync(userId, query, cancellationToken);

			if (!result.IsSuccess) {
				return Results.BadRequest(new { message = result.ErrorMessage });
			}

			return Results.Ok(result.Value);
		})
		.WithName("ListGenerations")
		.WithSummary("Get paginated list of user's generations")
		.WithDescription("Retrieves generation history with statistics and acceptance rates")
		.Produces<GenerationsListResponse>(StatusCodes.Status200OK)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized);

		// POST /api/generations
		group.MapPost("", async (
			[FromBody] GenerateFlashcardsRequest request,
			IGenerationService generationService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await generationService.GenerateFlashcardsAsync(
				userId, request, cancellationToken);

			if (!result.IsSuccess) {
				return Results.Problem(
					detail: result.ErrorMessage,
					statusCode: StatusCodes.Status500InternalServerError);
			}

			// Return 201 Created with location header
			return Results.Created($"/api/generations/{result.Value!.Id}", result.Value);
		})
		.WithName("GenerateFlashcards")
		.WithSummary("Generate flashcard suggestions using AI")
		.WithDescription("Generates flashcard suggestions from source text using OpenRouter AI models")
		.Produces<GenerationResponse>(StatusCodes.Status201Created)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized)
		.ProducesProblem(StatusCodes.Status500InternalServerError);

		// GET /api/generations/{id}
		group.MapGet("{id:long}", async (
			long id,
			IGenerationService generationService,
			ClaimsPrincipal user,
			CancellationToken cancellationToken) => {

			// Extract userId from JWT claims
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
				return Results.Unauthorized();
			}

			var result = await generationService.GetGenerationDetailsAsync(
				userId, id, cancellationToken);

			if (!result.IsSuccess) {
				return Results.NotFound(new { message = result.ErrorMessage });
			}

			return Results.Ok(result.Value);
		})
		.WithName("GetGenerationDetails")
		.WithSummary("Get detailed information about a specific generation")
		.WithDescription("Retrieves generation details with all associated flashcards")
		.Produces<GenerationDetailResponse>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status404NotFound)
		.Produces(StatusCodes.Status401Unauthorized);

		return app;
	}
}

