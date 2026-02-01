using _10xCards.Models.Requests;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Tests.Validators;

public class GenerateFlashcardsRequestValidatorTests {

	private IList<ValidationResult> ValidateModel(object model) {
		var validationResults = new List<ValidationResult>();
		var ctx = new ValidationContext(model, null, null);
		Validator.TryValidateObject(model, ctx, validationResults, true);
		return validationResults;
	}

	[Fact]
	public void GenerateFlashcardsRequest_ValidData_PassesValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000), // Exactly 1000 characters (minimum)
			Model = "gpt-4"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public void GenerateFlashcardsRequest_EmptySourceText_FailsValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = string.Empty,
			Model = "gpt-4"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Source text is required");
	}

	[Fact]
	public void GenerateFlashcardsRequest_SourceTextTooShort_FailsValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 999), // Just below minimum
			Model = "gpt-4"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Source text must be at least 1000 characters");
	}

	[Fact]
	public void GenerateFlashcardsRequest_SourceTextTooLong_FailsValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 10001), // Just above maximum
			Model = "gpt-4"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Source text must not exceed 10000 characters");
	}

	[Fact]
	public void GenerateFlashcardsRequest_SourceTextMaxLength_PassesValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 10000), // Exactly at maximum
			Model = "gpt-4"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public void GenerateFlashcardsRequest_EmptyModel_FailsValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = string.Empty
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Model is required");
	}

	[Fact]
	public void GenerateFlashcardsRequest_ModelTooLong_FailsValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = new string('a', 101) // Exceeds 100 characters
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Model identifier must not exceed 100 characters");
	}

	[Fact]
	public void GenerateFlashcardsRequest_ModelMaxLength_PassesValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = new string('a', 1000),
			Model = new string('a', 100) // Exactly 100 characters
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public void GenerateFlashcardsRequest_BothFieldsInvalid_FailsValidation() {
		// Arrange
		var request = new GenerateFlashcardsRequest {
			SourceText = string.Empty,
			Model = string.Empty
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().HaveCountGreaterThanOrEqualTo(2);
		results.Should().Contain(r => r.ErrorMessage == "Source text is required");
		results.Should().Contain(r => r.ErrorMessage == "Model is required");
	}
}

