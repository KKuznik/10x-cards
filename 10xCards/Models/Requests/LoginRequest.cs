using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for user login
/// Maps to: POST /api/auth/login
/// </summary>
public sealed class LoginRequest {
	[Required(ErrorMessage = "Email is required")]
	[EmailAddress(ErrorMessage = "Invalid email format")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "Password is required")]
	public string Password { get; set; } = string.Empty;
}

