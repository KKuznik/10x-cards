using _10xCards.Models.Requests;
using _10xCards.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _10xCards.Endpoints;

/// <summary>
/// Extension methods for mapping authentication endpoints
/// </summary>
public static class AuthEndpoints {
	/// <summary>
	/// Maps all authentication-related endpoints
	/// </summary>
	public static WebApplication MapAuthEndpoints(this WebApplication app) {
		var group = app.MapGroup("/api/auth")
			.WithTags("Authentication");

		// POST /api/auth/register
		group.MapPost("/register", async (
			[FromBody] RegisterRequest request,
			IAuthService authService,
			CancellationToken cancellationToken) => {

				var result = await authService.RegisterUserAsync(request, cancellationToken);

				if (!result.IsSuccess) {
					// Convert Dictionary<string, List<string>> to Dictionary<string, string[]>
					var errors = result.Errors?.ToDictionary(
						kvp => kvp.Key,
						kvp => kvp.Value.ToArray()
					) ?? new Dictionary<string, string[]>();

					return Results.ValidationProblem(
						errors,
						title: "One or more validation errors occurred.",
						statusCode: StatusCodes.Status400BadRequest
					);
				}

				return Results.Created($"/api/users/{result.Value!.UserId}", result.Value);
			})
		.WithName("RegisterUser")
		.WithSummary("Register a new user")
		.WithDescription("Creates a new user account and returns a JWT token for authentication")
		.Produces<Models.Responses.AuthResponse>(StatusCodes.Status201Created)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest);

		// POST /api/auth/login
		group.MapPost("/login", async (
			[FromBody] LoginRequest request,
			IAuthService authService,
			CancellationToken cancellationToken) => {

				var result = await authService.LoginUserAsync(request, cancellationToken);

				if (!result.IsSuccess) {
					return Results.Json(
						new Models.Responses.ErrorResponse {
							Message = result.ErrorMessage ?? "Invalid email or password"
						},
						statusCode: StatusCodes.Status401Unauthorized
					);
				}

				return Results.Ok(result.Value);
			})
		.WithName("Login")
		.WithSummary("Authenticate user")
		.WithDescription("Validates user credentials and returns JWT token")
		.Produces<Models.Responses.AuthResponse>(StatusCodes.Status200OK)
		.Produces<Models.Responses.ErrorResponse>(StatusCodes.Status401Unauthorized)
		.ProducesValidationProblem(StatusCodes.Status400BadRequest);

		// POST /api/auth/logout
		group.MapPost("/logout", async (
			HttpContext httpContext,
			IAuthService authService,
			CancellationToken cancellationToken) => {

				// Extract userId from authenticated user claims
				var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
					return Results.Json(
						new Models.Responses.ErrorResponse { Message = "Unauthorized" },
						statusCode: StatusCodes.Status401Unauthorized
					);
				}

				var result = await authService.LogoutUserAsync(userId, cancellationToken);

				if (!result.IsSuccess) {
					return Results.Json(
						new Models.Responses.ErrorResponse {
							Message = result.ErrorMessage ?? "An error occurred while processing logout",
							ErrorCode = "LOGOUT_ERROR"
						},
						statusCode: StatusCodes.Status500InternalServerError
					);
				}

				return Results.NoContent();
			})
		.RequireAuthorization()
		.WithName("Logout")
		.WithSummary("Logout current user")
		.WithDescription("Invalidates the current user session and logs the logout event for security auditing")
		.Produces(StatusCodes.Status204NoContent)
		.Produces<Models.Responses.ErrorResponse>(StatusCodes.Status401Unauthorized)
		.Produces<Models.Responses.ErrorResponse>(StatusCodes.Status500InternalServerError);

		return app;
	}
}

