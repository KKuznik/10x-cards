using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace _10xCards.Middleware;

/// <summary>
/// Global exception handler middleware that catches unhandled exceptions and returns RFC 7807 ProblemDetails
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware {
	private readonly RequestDelegate _next;
	private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
	private readonly IHostEnvironment _environment;

	public GlobalExceptionHandlerMiddleware(
		RequestDelegate next,
		ILogger<GlobalExceptionHandlerMiddleware> logger,
		IHostEnvironment environment) {
		_next = next;
		_logger = logger;
		_environment = environment;
	}

	public async Task InvokeAsync(HttpContext context) {
		try {
			await _next(context);
		} catch (Exception ex) {
			await HandleExceptionAsync(context, ex);
		}
	}

	private async Task HandleExceptionAsync(HttpContext context, Exception exception) {
		// Generate correlation ID for tracking
		var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

		// Log the error with correlation ID
		_logger.LogError(
			exception,
			"Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
			correlationId,
			context.Request.Path,
			context.Request.Method
		);

		// Determine status code based on exception type
		var statusCode = exception switch {
			ArgumentNullException => StatusCodes.Status400BadRequest,
			ArgumentException => StatusCodes.Status400BadRequest,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			InvalidOperationException => StatusCodes.Status400BadRequest,
			_ => StatusCodes.Status500InternalServerError
		};

		// Create ProblemDetails response
		var problemDetails = new ProblemDetails {
			Type = GetTypeUrl(statusCode),
			Title = GetTitle(statusCode),
			Status = statusCode,
			Detail = _environment.IsDevelopment() ? exception.Message : "An error occurred while processing your request.",
			Instance = context.Request.Path
		};

		// Add correlation ID to extensions
		problemDetails.Extensions["correlationId"] = correlationId;

		// Add stack trace in development mode
		if (_environment.IsDevelopment()) {
			problemDetails.Extensions["stackTrace"] = exception.StackTrace;
		}

		// Set response properties
		context.Response.StatusCode = statusCode;
		context.Response.ContentType = "application/problem+json";

		// Serialize and write response
		var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});

		await context.Response.WriteAsync(json);
	}

	private static string GetTypeUrl(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
		StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
		StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
		StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
		StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
		_ => "https://tools.ietf.org/html/rfc7231"
	};

	private static string GetTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "Bad Request",
		StatusCodes.Status401Unauthorized => "Unauthorized",
		StatusCodes.Status403Forbidden => "Forbidden",
		StatusCodes.Status404NotFound => "Not Found",
		StatusCodes.Status500InternalServerError => "An error occurred while processing your request.",
		_ => "An error occurred"
	};
}

