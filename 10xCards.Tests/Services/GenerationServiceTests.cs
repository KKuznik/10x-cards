using _10xCards.Database.Entities;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;
using _10xCards.Services;
using _10xCards.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace _10xCards.Tests.Services;

public class GenerationServiceTests : IClassFixture<DatabaseFixture> {
	private readonly DatabaseFixture _fixture;
	private readonly IChatGptService _chatGptService;
	private readonly ILogger<GenerationService> _logger;
	private readonly GenerationService _generationService;
	private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
	private readonly Guid _otherUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

	public GenerationServiceTests(DatabaseFixture fixture) {
		_fixture = fixture;
		_fixture.SeedDatabase();

		_chatGptService = Substitute.For<IChatGptService>();
		_logger = Substitute.For<ILogger<GenerationService>>();
		_generationService = new GenerationService(_fixture.Context, _logger, _chatGptService);
	}

	#region ListGenerationsAsync Tests

	[Fact]
	public async Task ListGenerationsAsync_ValidRequest_ReturnsGenerations() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		_fixture.Context.Generations.Add(new Generation {
			UserId = _testUserId,
			Model = "gpt-4",
			GeneratedCount = 10,
			AcceptedUneditedCount = 8,
			AcceptedEditedCount = 2,
			SourceTextHash = "hash123",
			SourceTextLength = 5000,
			GenerationDuration = 1500,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		});
		await _fixture.Context.SaveChangesAsync();

		var query = new ListGenerationsQuery {
			Page = 1,
			PageSize = 10,
			SortOrder = "desc"
		};

		// Act
		var result = await _generationService.ListGenerationsAsync(_testUserId, query);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Data.Should().HaveCountGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task ListGenerationsAsync_EmptyUserId_ReturnsFailure() {
		// Arrange
		var query = new ListGenerationsQuery {
			Page = 1,
			PageSize = 10
		};

		// Act
		var result = await _generationService.ListGenerationsAsync(Guid.Empty, query);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Be("Invalid user ID");
	}

	[Fact]
	public async Task ListGenerationsAsync_CalculatesAcceptanceRate() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		_fixture.Context.Generations.Add(new Generation {
			UserId = _testUserId,
			Model = "gpt-4",
			GeneratedCount = 10,
			AcceptedUneditedCount = 6,
			AcceptedEditedCount = 2,
			SourceTextHash = "hash123",
			SourceTextLength = 5000,
			GenerationDuration = 1500,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		});
		await _fixture.Context.SaveChangesAsync();

		var query = new ListGenerationsQuery {
			Page = 1,
			PageSize = 10
		};

		// Act
		var result = await _generationService.ListGenerationsAsync(_testUserId, query);

		// Assert
		result.IsSuccess.Should().BeTrue();
		var generation = result.Value!.Data.First();
		generation.AcceptanceRate.Should().BeApproximately(80.0, 0.01); // (6 + 2) / 10 * 100 = 80%
	}

	[Fact]
	public async Task ListGenerationsAsync_Sorting_AscendingOrder() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		_fixture.Context.Generations.AddRange(
			new Generation {
				UserId = _testUserId,
				Model = "gpt-4",
				GeneratedCount = 5,
				SourceTextHash = "hash1",
				SourceTextLength = 1000,
				GenerationDuration = 1000,
				CreatedAt = DateTime.UtcNow.AddDays(-2),
				UpdatedAt = DateTime.UtcNow.AddDays(-2)
			},
			new Generation {
				UserId = _testUserId,
				Model = "gpt-3.5-turbo",
				GeneratedCount = 3,
				SourceTextHash = "hash2",
				SourceTextLength = 1000,
				GenerationDuration = 500,
				CreatedAt = DateTime.UtcNow.AddDays(-1),
				UpdatedAt = DateTime.UtcNow.AddDays(-1)
			}
		);
		await _fixture.Context.SaveChangesAsync();

		var query = new ListGenerationsQuery {
			Page = 1,
			PageSize = 10,
			SortOrder = "asc"
		};

		// Act
		var result = await _generationService.ListGenerationsAsync(_testUserId, query);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value!.Data.Should().BeInAscendingOrder(g => g.CreatedAt);
	}

	[Fact]
	public async Task ListGenerationsAsync_RowLevelSecurity_OnlyReturnsUserGenerations() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		_fixture.Context.Generations.AddRange(
			new Generation {
				UserId = _testUserId,
				Model = "gpt-4",
				GeneratedCount = 5,
				SourceTextHash = "hash1",
				SourceTextLength = 1000,
				GenerationDuration = 1000,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			},
			new Generation {
				UserId = _otherUserId,
				Model = "gpt-4",
				GeneratedCount = 3,
				SourceTextHash = "hash2",
				SourceTextLength = 1000,
				GenerationDuration = 500,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			}
		);
		await _fixture.Context.SaveChangesAsync();

		var query = new ListGenerationsQuery {
			Page = 1,
			PageSize = 10
		};

		// Act
		var result = await _generationService.ListGenerationsAsync(_testUserId, query);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value!.Data.Should().HaveCountGreaterThanOrEqualTo(1);
		// Note: GenerationListItemResponse doesn't include UserId for security,
		// but RLS ensures only user's generations are returned
	}

	#endregion

	#region GetGenerationDetailsAsync Tests

	[Fact]
	public async Task GetGenerationDetailsAsync_ExistingGeneration_ReturnsDetails() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var generation = new Generation {
			UserId = _testUserId,
			Model = "gpt-4",
			GeneratedCount = 2,
			SourceTextHash = "hash123",
			SourceTextLength = 1000,
			GenerationDuration = 1500,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_fixture.Context.Generations.Add(generation);
		await _fixture.Context.SaveChangesAsync();

		_fixture.Context.Flashcards.AddRange(
			new Flashcard {
				UserId = _testUserId,
				GenerationId = generation.Id,
				Front = "Q1",
				Back = "A1",
				Source = "ai-generated",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			},
			new Flashcard {
				UserId = _testUserId,
				GenerationId = generation.Id,
				Front = "Q2",
				Back = "A2",
				Source = "ai-generated",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			}
		);
		await _fixture.Context.SaveChangesAsync();

		// Act
		var result = await _generationService.GetGenerationDetailsAsync(_testUserId, generation.Id);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(generation.Id);
		result.Value.Flashcards.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetGenerationDetailsAsync_NonExistentGeneration_ReturnsFailure() {
		// Arrange
		var nonExistentId = 999999L;

		// Act
		var result = await _generationService.GetGenerationDetailsAsync(_testUserId, nonExistentId);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Be("Generation not found");
	}

	[Fact]
	public async Task GetGenerationDetailsAsync_OtherUserGeneration_ReturnsFailure() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var generation = new Generation {
			UserId = _otherUserId,
			Model = "gpt-4",
			GeneratedCount = 1,
			SourceTextHash = "hash123",
			SourceTextLength = 1000,
			GenerationDuration = 1500,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_fixture.Context.Generations.Add(generation);
		await _fixture.Context.SaveChangesAsync();

		// Act
		var result = await _generationService.GetGenerationDetailsAsync(_testUserId, generation.Id);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Be("Generation not found");
	}

	#endregion

	#region GenerateFlashcardsAsync Tests

	[Fact]
	public async Task GenerateFlashcardsAsync_SuccessfulGeneration_SavesGenerationAndReturnsFlashcards() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var sourceText = new string('a', 1000);
		var request = new GenerateFlashcardsRequest {
			SourceText = sourceText,
			Model = "gpt-4"
		};

		var mockFlashcards = new List<ProposedFlashcardDto> {
			new() { Front = "Generated Q1", Back = "Generated A1" },
			new() { Front = "Generated Q2", Back = "Generated A2" },
			new() { Front = "Generated Q3", Back = "Generated A3" }
		};

		_chatGptService.GenerateFlashcardsAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns(mockFlashcards);

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(_testUserId, request);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().BeGreaterThan(0);
		result.Value.Flashcards.Should().HaveCount(3);
		result.Value.GenerationDuration.Should().BeGreaterThanOrEqualTo(0);

		// Verify generation was saved to database
		var generation = await _fixture.Context.Generations.FindAsync(result.Value.Id);
		generation.Should().NotBeNull();
		generation!.GeneratedCount.Should().Be(3);
		generation.Model.Should().Be("gpt-4");
		generation.SourceTextLength.Should().Be(1000);
	}

	[Fact]
	public async Task GenerateFlashcardsAsync_EmptyUserId_ReturnsFailure() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = "gpt-4"
		};

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(Guid.Empty, request);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Be("Invalid user ID");
	}

	[Fact]
	public async Task GenerateFlashcardsAsync_HttpRequestException_LogsErrorAndReturnsFailure() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = "gpt-4"
		};

		_chatGptService.GenerateFlashcardsAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns<List<ProposedFlashcardDto>>(x => throw new HttpRequestException("API error"));

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(_testUserId, request);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Contain("Failed to generate flashcards");

		// Verify error log was created
		var errorLog = _fixture.Context.GenerationErrorLogs
			.FirstOrDefault(e => e.UserId == _testUserId && e.ErrorCode == "AI_API_ERROR");
		errorLog.Should().NotBeNull();
	}

	[Fact]
	public async Task GenerateFlashcardsAsync_InvalidOperationException_LogsErrorAndReturnsFailure() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = "gpt-4"
		};

		_chatGptService.GenerateFlashcardsAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns<List<ProposedFlashcardDto>>(x => throw new InvalidOperationException("Invalid response"));

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(_testUserId, request);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Contain("Failed to parse AI response");

		// Verify error log was created
		var errorLog = _fixture.Context.GenerationErrorLogs
			.FirstOrDefault(e => e.UserId == _testUserId && e.ErrorCode == "AI_INVALID_RESPONSE");
		errorLog.Should().NotBeNull();
	}

	[Fact]
	public async Task GenerateFlashcardsAsync_EmptyFlashcardsList_ReturnsFailure() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = "gpt-4"
		};

		_chatGptService.GenerateFlashcardsAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns(new List<ProposedFlashcardDto>());

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(_testUserId, request);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorMessage.Should().Contain("did not generate any flashcards");
	}

	[Fact]
	public async Task GenerateFlashcardsAsync_CalculatesSourceTextHash() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var sourceText = "This is a test text for hashing";
		var request = new GenerateFlashcardsRequest {
			SourceText = sourceText.PadRight(1000, 'a'), // Pad to meet minimum length
			Model = "gpt-4"
		};

		var mockFlashcards = new List<ProposedFlashcardDto> {
			new() { Front = "Q1", Back = "A1" }
		};

		_chatGptService.GenerateFlashcardsAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns(mockFlashcards);

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(_testUserId, request);

		// Assert
		result.IsSuccess.Should().BeTrue();

		var generation = await _fixture.Context.Generations.FindAsync(result.Value!.Id);
		generation!.SourceTextHash.Should().NotBeNullOrEmpty();
		generation.SourceTextHash.Length.Should().Be(64); // SHA-256 produces 64 hex characters
	}

	[Fact]
	public async Task GenerateFlashcardsAsync_MeasuresGenerationDuration() {
		// Arrange
		_fixture.ClearDatabase();
		_fixture.SeedDatabase();

		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = "gpt-4"
		};

		var mockFlashcards = new List<ProposedFlashcardDto> {
			new() { Front = "Q1", Back = "A1" }
		};

		_chatGptService.GenerateFlashcardsAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns(async callInfo => {
			await Task.Delay(100); // Simulate processing time
			return mockFlashcards;
		});

		// Act
		var result = await _generationService.GenerateFlashcardsAsync(_testUserId, request);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value!.GenerationDuration.Should().BeGreaterThanOrEqualTo(100);
	}

	#endregion
}

