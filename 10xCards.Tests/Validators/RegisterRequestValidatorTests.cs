using _10xCards.Models.Requests;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Tests.Validators;

public class RegisterRequestValidatorTests {
	
	private IList<ValidationResult> ValidateModel(object model) {
		var validationResults = new List<ValidationResult>();
		var ctx = new ValidationContext(model, null, null);
		Validator.TryValidateObject(model, ctx, validationResults, true);
		return validationResults;
	}

	[Fact]
	public void RegisterRequest_ValidData_PassesValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "Password123!",
			ConfirmPassword = "Password123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public void RegisterRequest_EmptyEmail_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = string.Empty,
			Password = "Password123!",
			ConfirmPassword = "Password123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().ContainSingle()
			.Which.ErrorMessage.Should().Be("Email is required");
	}

	[Fact]
	public void RegisterRequest_InvalidEmailFormat_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "not-an-email",
			Password = "Password123!",
			ConfirmPassword = "Password123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().ContainSingle()
			.Which.ErrorMessage.Should().Be("Invalid email format");
	}

	[Fact]
	public void RegisterRequest_EmailTooLong_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = new string('a', 247) + "@test.com", // 256 characters total
			Password = "Password123!",
			ConfirmPassword = "Password123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Email must not exceed 255 characters");
	}

	[Fact]
	public void RegisterRequest_EmptyPassword_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = string.Empty,
			ConfirmPassword = string.Empty
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password is required");
	}

	[Fact]
	public void RegisterRequest_PasswordTooShort_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "Pass1!",
			ConfirmPassword = "Pass1!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password must be at least 8 characters");
	}

	[Fact]
	public void RegisterRequest_PasswordTooLong_FailsValidation() {
		// Arrange
		var longPassword = new string('a', 96) + "A1!ab"; // 101 characters with required complexity
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = longPassword,
			ConfirmPassword = longPassword
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password must not exceed 100 characters");
	}

	[Fact]
	public void RegisterRequest_PasswordMissingUppercase_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "password123!",
			ConfirmPassword = "password123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password must contain uppercase, lowercase, number, and special character");
	}

	[Fact]
	public void RegisterRequest_PasswordMissingLowercase_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "PASSWORD123!",
			ConfirmPassword = "PASSWORD123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password must contain uppercase, lowercase, number, and special character");
	}

	[Fact]
	public void RegisterRequest_PasswordMissingNumber_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "Password!",
			ConfirmPassword = "Password!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password must contain uppercase, lowercase, number, and special character");
	}

	[Fact]
	public void RegisterRequest_PasswordMissingSpecialCharacter_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "Password123",
			ConfirmPassword = "Password123"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password must contain uppercase, lowercase, number, and special character");
	}

	[Fact]
	public void RegisterRequest_ConfirmPasswordMismatch_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "Password123!",
			ConfirmPassword = "DifferentPassword123!"
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password and confirmation password must match");
	}

	[Fact]
	public void RegisterRequest_EmptyConfirmPassword_FailsValidation() {
		// Arrange
		var request = new RegisterRequest {
			Email = "test@example.com",
			Password = "Password123!",
			ConfirmPassword = string.Empty
		};

		// Act
		var results = ValidateModel(request);

		// Assert
		results.Should().Contain(r => r.ErrorMessage == "Password confirmation is required");
	}
}

