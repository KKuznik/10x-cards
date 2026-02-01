using _10xCards.Database.Entities;
using _10xCards.Models.Requests;
using _10xCards.Services;
using _10xCards.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace _10xCards.Tests.Services;

public class FlashcardServiceTests : IClassFixture<DatabaseFixture> {
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<FlashcardService> _logger;
    private readonly FlashcardService _flashcardService;
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _otherUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public FlashcardServiceTests(DatabaseFixture fixture) {
        _fixture = fixture;
        _fixture.SeedDatabase();

        _logger = Substitute.For<ILogger<FlashcardService>>();
        _flashcardService = new FlashcardService(_fixture.Context, _logger);
    }

    #region ListFlashcardsAsync Tests

    [Fact]
    public async Task ListFlashcardsAsync_ValidRequest_ReturnsFlashcards() {
        // Arrange
        _fixture.Context.Flashcards.Add(new Flashcard {
            UserId = _testUserId,
            Front = "Question 1",
            Back = "Answer 1",
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _fixture.Context.SaveChangesAsync();

        var query = new ListFlashcardsQuery {
            Page = 1,
            PageSize = 10,
            SortBy = "createdAt",
            SortOrder = "desc"
        };

        // Act
        var result = await _flashcardService.ListFlashcardsAsync(_testUserId, query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Value.Pagination.TotalItems.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ListFlashcardsAsync_EmptyUserId_ReturnsFailure() {
        // Arrange
        var query = new ListFlashcardsQuery {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _flashcardService.ListFlashcardsAsync(Guid.Empty, query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid user ID");
    }

    [Fact]
    public async Task ListFlashcardsAsync_NullQuery_ReturnsFailure() {
        // Arrange & Act
        var result = await _flashcardService.ListFlashcardsAsync(_testUserId, null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Query parameters are required");
    }

    [Fact]
    public async Task ListFlashcardsAsync_WithSourceFilter_ReturnsFilteredResults() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        _fixture.Context.Flashcards.AddRange(
            new Flashcard {
                UserId = _testUserId,
                Front = "Manual Question",
                Back = "Manual Answer",
                Source = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Flashcard {
                UserId = _testUserId,
                Front = "AI Question",
                Back = "AI Answer",
                Source = "ai-generated",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _fixture.Context.SaveChangesAsync();

        var query = new ListFlashcardsQuery {
            Page = 1,
            PageSize = 10,
            Source = "manual"
        };

        // Act
        var result = await _flashcardService.ListFlashcardsAsync(_testUserId, query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().AllSatisfy(f => f.Source.Should().Be("manual"));
    }

    [Fact]
    public async Task ListFlashcardsAsync_WithSearchFilter_ReturnsMatchingResults() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        _fixture.Context.Flashcards.AddRange(
            new Flashcard {
                UserId = _testUserId,
                Front = "What is C#?",
                Back = "Programming language",
                Source = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Flashcard {
                UserId = _testUserId,
                Front = "What is Python?",
                Back = "Another programming language",
                Source = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _fixture.Context.SaveChangesAsync();

        var query = new ListFlashcardsQuery {
            Page = 1,
            PageSize = 10,
            Search = "Python"
        };

        // Act
        var result = await _flashcardService.ListFlashcardsAsync(_testUserId, query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().ContainSingle();
        result.Value.Data[0].Front.Should().Contain("Python");
    }

    [Fact]
    public async Task ListFlashcardsAsync_RowLevelSecurity_OnlyReturnsUserFlashcards() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        _fixture.Context.Flashcards.AddRange(
            new Flashcard {
                UserId = _testUserId,
                Front = "User 1 Question",
                Back = "User 1 Answer",
                Source = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Flashcard {
                UserId = _otherUserId,
                Front = "User 2 Question",
                Back = "User 2 Answer",
                Source = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _fixture.Context.SaveChangesAsync();

        var query = new ListFlashcardsQuery {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _flashcardService.ListFlashcardsAsync(_testUserId, query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().AllSatisfy(f => f.Front.Should().Contain("User 1"));
        result.Value.Data.Should().NotContain(f => f.Front.Contains("User 2"));
    }

    [Fact]
    public async Task ListFlashcardsAsync_Pagination_ReturnsCorrectPage() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        for (int i = 1; i <= 15; i++) {
            _fixture.Context.Flashcards.Add(new Flashcard {
                UserId = _testUserId,
                Front = $"Question {i}",
                Back = $"Answer {i}",
                Source = "manual",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _fixture.Context.SaveChangesAsync();

        var query = new ListFlashcardsQuery {
            Page = 2,
            PageSize = 10
        };

        // Act
        var result = await _flashcardService.ListFlashcardsAsync(_testUserId, query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(5); // 15 total, page 2 with size 10 = 5 items
        result.Value.Pagination.CurrentPage.Should().Be(2);
        result.Value.Pagination.TotalPages.Should().Be(2);
        result.Value.Pagination.TotalItems.Should().Be(15);
    }

    #endregion

    #region GetFlashcardAsync Tests

    [Fact]
    public async Task GetFlashcardAsync_ExistingFlashcard_ReturnsFlashcard() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var flashcard = new Flashcard {
            UserId = _testUserId,
            Front = "Test Question",
            Back = "Test Answer",
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.Context.Flashcards.Add(flashcard);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _flashcardService.GetFlashcardAsync(_testUserId, flashcard.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Front.Should().Be("Test Question");
        result.Value.Back.Should().Be("Test Answer");
    }

    [Fact]
    public async Task GetFlashcardAsync_NonExistentFlashcard_ReturnsFailure() {
        // Arrange
        var nonExistentId = 999999L;

        // Act
        var result = await _flashcardService.GetFlashcardAsync(_testUserId, nonExistentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Flashcard not found");
    }

    [Fact]
    public async Task GetFlashcardAsync_OtherUserFlashcard_ReturnsFailure() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var flashcard = new Flashcard {
            UserId = _otherUserId,
            Front = "Other User Question",
            Back = "Other User Answer",
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.Context.Flashcards.Add(flashcard);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _flashcardService.GetFlashcardAsync(_testUserId, flashcard.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Flashcard not found");
    }

    [Fact]
    public async Task GetFlashcardAsync_EmptyUserId_ReturnsFailure() {
        // Arrange & Act
        var result = await _flashcardService.GetFlashcardAsync(Guid.Empty, 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid user ID");
    }

    #endregion

    #region CreateFlashcardAsync Tests

    [Fact]
    public async Task CreateFlashcardAsync_ValidRequest_CreatesFlashcard() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var request = new CreateFlashcardRequest {
            Front = "New Question",
            Back = "New Answer"
        };

        // Act
        var result = await _flashcardService.CreateFlashcardAsync(_testUserId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Front.Should().Be("New Question");
        result.Value.Back.Should().Be("New Answer");
        result.Value.Source.Should().Be("manual");
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateFlashcardAsync_EmptyUserId_ReturnsFailure() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = "Question",
            Back = "Answer"
        };

        // Act
        var result = await _flashcardService.CreateFlashcardAsync(Guid.Empty, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid user ID");
    }

    [Fact]
    public async Task CreateFlashcardAsync_SavedToDatabase_CanBeRetrieved() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var request = new CreateFlashcardRequest {
            Front = "Persistence Test",
            Back = "Should be saved"
        };

        // Act
        var createResult = await _flashcardService.CreateFlashcardAsync(_testUserId, request);
        var getResult = await _flashcardService.GetFlashcardAsync(_testUserId, createResult.Value!.Id);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.Front.Should().Be("Persistence Test");
    }

    #endregion

    #region CreateFlashcardsBatchAsync Tests

    [Fact]
    public async Task CreateFlashcardsBatchAsync_ValidRequest_CreatesMultipleFlashcards() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        // Create a generation for the batch
        var generation = new Generation {
            UserId = _testUserId,
            Model = "gpt-4",
            GeneratedCount = 3,
            SourceTextHash = "test-hash",
            SourceTextLength = 100,
            GenerationDuration = 1000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.Context.Generations.Add(generation);
        await _fixture.Context.SaveChangesAsync();

        var request = new CreateFlashcardsBatchRequest {
            GenerationId = generation.Id,
            Flashcards = new List<BatchFlashcardItem> {
                new() { Front = "Batch Q1", Back = "Batch A1", Source = "ai-full" },
                new() { Front = "Batch Q2", Back = "Batch A2", Source = "ai-full" },
                new() { Front = "Batch Q3", Back = "Batch A3", Source = "ai-edited" }
            }
        };

        // Act
        var result = await _flashcardService.CreateFlashcardsBatchAsync(_testUserId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Created.Should().Be(3);
        result.Value.Flashcards.Should().HaveCount(3);
        result.Value.Flashcards[0].Source.Should().Be("ai-full");
        result.Value.Flashcards[1].Source.Should().Be("ai-full");
        result.Value.Flashcards[2].Source.Should().Be("ai-edited");
        result.Value.Flashcards.Should().AllSatisfy(f => {
            f.GenerationId.Should().Be(generation.Id);
        });
    }

    [Fact]
    public async Task CreateFlashcardsBatchAsync_EmptyList_ReturnsFailure() {
        // Arrange
        var request = new CreateFlashcardsBatchRequest {
            GenerationId = 1,
            Flashcards = new List<BatchFlashcardItem>()
        };

        // Act
        var result = await _flashcardService.CreateFlashcardsBatchAsync(_testUserId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("At least one flashcard is required");
    }

    [Fact]
    public async Task CreateFlashcardsBatchAsync_EmptyUserId_ReturnsFailure() {
        // Arrange
        var request = new CreateFlashcardsBatchRequest {
            GenerationId = 1,
            Flashcards = new List<BatchFlashcardItem> {
                new() { Front = "Q", Back = "A" }
            }
        };

        // Act
        var result = await _flashcardService.CreateFlashcardsBatchAsync(Guid.Empty, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid user ID");
    }

    #endregion

    #region UpdateFlashcardAsync Tests

    [Fact]
    public async Task UpdateFlashcardAsync_ExistingFlashcard_UpdatesSuccessfully() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var originalTime = DateTime.UtcNow.AddHours(-1);
        var flashcard = new Flashcard {
            UserId = _testUserId,
            Front = "Original Question",
            Back = "Original Answer",
            Source = "manual",
            CreatedAt = originalTime,
            UpdatedAt = originalTime
        };
        _fixture.Context.Flashcards.Add(flashcard);
        await _fixture.Context.SaveChangesAsync();

        var request = new UpdateFlashcardRequest {
            Front = "Updated Question",
            Back = "Updated Answer"
        };

        // Act
        var result = await _flashcardService.UpdateFlashcardAsync(_testUserId, flashcard.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Front.Should().Be("Updated Question");
        result.Value.Back.Should().Be("Updated Answer");
        result.Value.UpdatedAt.Should().BeAfter(originalTime);
    }

    [Fact]
    public async Task UpdateFlashcardAsync_NonExistentFlashcard_ReturnsFailure() {
        // Arrange
        var request = new UpdateFlashcardRequest {
            Front = "Updated",
            Back = "Updated"
        };

        // Act
        var result = await _flashcardService.UpdateFlashcardAsync(_testUserId, 999999L, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Flashcard not found or does not belong to user");
    }

    [Fact]
    public async Task UpdateFlashcardAsync_OtherUserFlashcard_ReturnsFailure() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var flashcard = new Flashcard {
            UserId = _otherUserId,
            Front = "Other User Question",
            Back = "Other User Answer",
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.Context.Flashcards.Add(flashcard);
        await _fixture.Context.SaveChangesAsync();

        var request = new UpdateFlashcardRequest {
            Front = "Hacked",
            Back = "Hacked"
        };

        // Act
        var result = await _flashcardService.UpdateFlashcardAsync(_testUserId, flashcard.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Flashcard not found or does not belong to user");
    }

    #endregion

    #region DeleteFlashcardAsync Tests

    [Fact]
    public async Task DeleteFlashcardAsync_ExistingFlashcard_DeletesSuccessfully() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var flashcard = new Flashcard {
            UserId = _testUserId,
            Front = "To Delete",
            Back = "Will be removed",
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.Context.Flashcards.Add(flashcard);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _flashcardService.DeleteFlashcardAsync(_testUserId, flashcard.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        // Verify deletion
        var getResult = await _flashcardService.GetFlashcardAsync(_testUserId, flashcard.Id);
        getResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFlashcardAsync_NonExistentFlashcard_ReturnsFailure() {
        // Arrange & Act
        var result = await _flashcardService.DeleteFlashcardAsync(_testUserId, 999999L);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Flashcard not found or does not belong to user");
    }

    [Fact]
    public async Task DeleteFlashcardAsync_OtherUserFlashcard_ReturnsFailure() {
        // Arrange
        _fixture.ClearDatabase();
        _fixture.SeedDatabase();

        var flashcard = new Flashcard {
            UserId = _otherUserId,
            Front = "Protected",
            Back = "Cannot delete",
            Source = "manual",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.Context.Flashcards.Add(flashcard);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _flashcardService.DeleteFlashcardAsync(_testUserId, flashcard.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Flashcard not found or does not belong to user");
    }

    #endregion
}

