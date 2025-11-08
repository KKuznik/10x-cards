using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for user registration
/// Maps to: POST /api/auth/register
/// </summary>
public sealed class RegisterRequest {
	[Required(ErrorMessage = "Email is required")]
	[EmailAddress(ErrorMessage = "Invalid email format")]
	[MaxLength(255, ErrorMessage = "Email must not exceed 255 characters")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "Password is required")]
	[MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
	[MaxLength(100, ErrorMessage = "Password must not exceed 100 characters")]
	[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
		ErrorMessage = "Password must contain uppercase, lowercase, number, and special character")]
	public string Password { get; set; } = string.Empty;

	[Required(ErrorMessage = "Password confirmation is required")]
	[Compare(nameof(Password), ErrorMessage = "Password and confirmation password must match")]
	public string ConfirmPassword { get; set; } = string.Empty;
}

