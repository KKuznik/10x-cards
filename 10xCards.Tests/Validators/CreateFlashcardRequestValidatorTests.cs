using _10xCards.Models.Requests;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Tests.Validators;

public class CreateFlashcardRequestValidatorTests {

    private IList<ValidationResult> ValidateModel(object model) {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void CreateFlashcardRequest_ValidData_PassesValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = "What is the capital of France?",
            Back = "Paris"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateFlashcardRequest_EmptyFront_FailsValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = string.Empty,
            Back = "Paris"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.ErrorMessage == "Front is required");
    }

    [Fact]
    public void CreateFlashcardRequest_FrontTooLong_FailsValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = new string('a', 201),
            Back = "Answer"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.ErrorMessage == "Front must not exceed 200 characters");
    }

    [Fact]
    public void CreateFlashcardRequest_FrontMaxLength_PassesValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = new string('a', 200),
            Back = "Answer"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateFlashcardRequest_EmptyBack_FailsValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = "Question",
            Back = string.Empty
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.ErrorMessage == "Back is required");
    }

    [Fact]
    public void CreateFlashcardRequest_BackTooLong_FailsValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = "Question",
            Back = new string('a', 501)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().Contain(r => r.ErrorMessage == "Back must not exceed 500 characters");
    }

    [Fact]
    public void CreateFlashcardRequest_BackMaxLength_PassesValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = "Question",
            Back = new string('a', 500)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateFlashcardRequest_BothFieldsEmpty_FailsValidation() {
        // Arrange
        var request = new CreateFlashcardRequest {
            Front = string.Empty,
            Back = string.Empty
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        results.Should().HaveCountGreaterThanOrEqualTo(2);
        results.Should().Contain(r => r.ErrorMessage == "Front is required");
        results.Should().Contain(r => r.ErrorMessage == "Back is required");
    }
}

