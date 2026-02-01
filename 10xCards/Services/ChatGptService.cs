using _10xCards.Models.Responses;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _10xCards.Services;

/// <summary>
/// Service for ChatGPT (OpenAI) integration
/// </summary>
public sealed class ChatGptService : IChatGptService {
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _configuration;
	private readonly ILogger<ChatGptService> _logger;

	public ChatGptService(
		HttpClient httpClient,
		IConfiguration configuration,
		ILogger<ChatGptService> logger) {
		_httpClient = httpClient;
		_configuration = configuration;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<List<ProposedFlashcardDto>> GenerateFlashcardsAsync(
		string sourceText,
		string model,
		CancellationToken cancellationToken = default) {

		// Guard clause: validate inputs
		if (string.IsNullOrWhiteSpace(sourceText)) {
			throw new ArgumentException("Source text cannot be empty", nameof(sourceText));
		}

		if (string.IsNullOrWhiteSpace(model)) {
			throw new ArgumentException("Model cannot be empty", nameof(model));
		}

		// Get API key from configuration
		var apiKey = _configuration["OpenAI:ApiKey"]
			?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

		if (string.IsNullOrWhiteSpace(apiKey)) {
			_logger.LogError("OpenAI API key not found in configuration");
			throw new InvalidOperationException("OpenAI API key is not configured");
		}

		try {
			// Construct the request
			var requestBody = new OpenAIRequest {
				Model = model,
				Messages = new List<OpenAIMessage> {
					new OpenAIMessage {
						Role = "system",
						Content = @"You are a flashcard generation assistant. Generate clear, concise flashcards from the provided text.

INSTRUCTIONS:
- Create 5-15 flashcards based on the most important concepts
- Each flashcard should have a 'front' (question) and 'back' (answer)
- Questions should be specific and clear
- Answers should be concise but complete
- Focus on key concepts, definitions, processes, and relationships
- Return ONLY valid JSON in the exact format below, with no additional text or explanations

REQUIRED JSON FORMAT:
{
  ""flashcards"": [
    {
      ""front"": ""Question text here"",
      ""back"": ""Answer text here""
    }
  ]
}"
					},
					new OpenAIMessage {
						Role = "user",
						Content = $"Generate flashcards from this text:\n\n{sourceText}"
					}
				}
			};

			var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			});

			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			// Set authorization header
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

			_logger.LogInformation("Sending flashcard generation request to OpenAI with model {Model}", model);

			// Make the API call
			var response = await _httpClient.PostAsync(
				"chat/completions",
				content,
				cancellationToken);

			// Check for HTTP errors
			if (!response.IsSuccessStatusCode) {
				var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogError(
					"OpenAI API returned error status {StatusCode}: {ErrorContent}",
					response.StatusCode,
					errorContent);

				throw new HttpRequestException(
					$"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
			}

			// Parse the response
			var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
			_logger.LogDebug("Received response from OpenAI: {Response}", responseContent);

			var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(
				responseContent,
				new JsonSerializerOptions {
					PropertyNameCaseInsensitive = true
				});

			if (openAIResponse?.Choices == null || openAIResponse.Choices.Count == 0) {
				_logger.LogError("OpenAI response has no choices");
				throw new InvalidOperationException("OpenAI API returned no choices");
			}

			var messageContent = openAIResponse.Choices[0].Message?.Content;
			if (string.IsNullOrWhiteSpace(messageContent)) {
				_logger.LogError("OpenAI response message content is empty");
				throw new InvalidOperationException("OpenAI API returned empty message content");
			}

			// Parse the flashcards from the message content
			// The AI should return JSON with a "flashcards" array
			var flashcardsResponse = JsonSerializer.Deserialize<FlashcardsWrapper>(
				messageContent,
				new JsonSerializerOptions {
					PropertyNameCaseInsensitive = true
				});

			if (flashcardsResponse?.Flashcards == null || flashcardsResponse.Flashcards.Count == 0) {
				_logger.LogError("Failed to parse flashcards from AI response: {Content}", messageContent);
				throw new InvalidOperationException("AI response did not contain valid flashcards");
			}

			_logger.LogInformation(
				"Successfully generated {Count} flashcards using model {Model}",
				flashcardsResponse.Flashcards.Count,
				model);

			return flashcardsResponse.Flashcards;
		} catch (TaskCanceledException ex) {
			_logger.LogError(ex, "OpenAI API request timed out for model {Model}", model);
			throw new HttpRequestException("OpenAI API request timed out", ex);
		} catch (HttpRequestException ex) {
			_logger.LogError(ex, "HTTP error while calling OpenAI API with model {Model}", model);
			throw;
		} catch (JsonException ex) {
			_logger.LogError(ex, "Failed to parse OpenAI API response for model {Model}", model);
			throw new InvalidOperationException("Failed to parse AI response", ex);
		}
	}

	// Internal classes for OpenAI API communication
	private sealed class OpenAIRequest {
		public string Model { get; set; } = string.Empty;
		public List<OpenAIMessage> Messages { get; set; } = new();
	}

	private sealed class OpenAIMessage {
		public string Role { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
	}

	private sealed class OpenAIResponse {
		public List<OpenAIChoice> Choices { get; set; } = new();
	}

	private sealed class OpenAIChoice {
		public OpenAIMessage? Message { get; set; }
	}

	private sealed class FlashcardsWrapper {
		public List<ProposedFlashcardDto> Flashcards { get; set; } = new();
	}
}

