using _10xCards.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace _10xCards.Tests.Services;

public class ChatGptServiceTests {
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatGptService> _logger;

    public ChatGptServiceTests() {
        var configData = new Dictionary<string, string?> {
            { "OpenAI:ApiKey", "test-api-key-12345" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _logger = Substitute.For<ILogger<ChatGptService>>();
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string responseContent) {
        var mockHandler = new MockHttpMessageHandler(statusCode, responseContent);
        return new HttpClient(mockHandler) {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_ValidResponse_ReturnsFlashcards() {
        // Arrange
        var responseJson = @"{
			""choices"": [
				{
					""message"": {
						""content"": ""{\n  \""flashcards\"": [\n    {\""front\"": \""Question 1\"", \""back\"": \""Answer 1\""},\n    {\""front\"": \""Question 2\"", \""back\"": \""Answer 2\""}\n  ]\n}""
					}
				}
			]
		}";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act
        var result = await service.GenerateFlashcardsAsync("Test source text", "gpt-4");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Front.Should().Be("Question 1");
        result[0].Back.Should().Be("Answer 1");
        result[1].Front.Should().Be("Question 2");
        result[1].Back.Should().Be("Answer 2");
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_EmptySourceText_ThrowsArgumentException() {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{}");
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GenerateFlashcardsAsync(string.Empty, "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_NullSourceText_ThrowsArgumentException() {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{}");
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GenerateFlashcardsAsync(null!, "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_EmptyModel_ThrowsArgumentException() {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{}");
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", string.Empty));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_NullModel_ThrowsArgumentException() {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{}");
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", null!));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_MissingApiKey_ThrowsInvalidOperationException() {
        // Arrange
        var configWithoutKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{}");
        var service = new ChatGptService(httpClient, configWithoutKey, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_HttpError404_ThrowsHttpRequestException() {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.NotFound, "Not found");
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_HttpError500_ThrowsHttpRequestException() {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Server error");
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_InvalidJsonResponse_ThrowsInvalidOperationException() {
        // Arrange
        var invalidJson = "This is not valid JSON";
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, invalidJson);
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_MissingChoicesInResponse_ThrowsInvalidOperationException() {
        // Arrange
        var responseJson = @"{ ""id"": ""test"" }"; // Missing choices
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_EmptyChoicesArray_ThrowsInvalidOperationException() {
        // Arrange
        var responseJson = @"{ ""choices"": [] }";
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_InvalidFlashcardJsonInContent_ThrowsInvalidOperationException() {
        // Arrange
        var responseJson = @"{
			""choices"": [
				{
					""message"": {
						""content"": ""This is not valid JSON array""
					}
				}
			]
		}";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateFlashcardsAsync("Test text", "gpt-4"));
    }

    [Fact]
    public async Task GenerateFlashcardsAsync_ValidResponse_ReturnsMultipleFlashcards() {
        // Arrange
        var responseJson = @"{
			""choices"": [
				{
					""message"": {
						""content"": ""{\n  \""flashcards\"": [\n    {\""front\"": \""Q1\"", \""back\"": \""A1\""},\n    {\""front\"": \""Q2\"", \""back\"": \""A2\""},\n    {\""front\"": \""Q3\"", \""back\"": \""A3\""}\n  ]\n}""
					}
				}
			]
		}";

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var service = new ChatGptService(httpClient, _configuration, _logger);

        // Act
        var result = await service.GenerateFlashcardsAsync("Long source text", "gpt-4");

        // Assert
        result.Should().HaveCount(3);
    }
}

/// <summary>
/// Mock HttpMessageHandler for testing HTTP requests
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler {
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseContent;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent) {
        _statusCode = statusCode;
        _responseContent = responseContent;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var response = new HttpResponseMessage(_statusCode) {
            Content = new StringContent(_responseContent)
        };

        return Task.FromResult(response);
    }
}

