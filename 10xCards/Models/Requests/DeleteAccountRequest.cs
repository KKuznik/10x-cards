using System.ComponentModel.DataAnnotations;

namespace _10xCards.Models.Requests;

/// <summary>
/// Request model for account deletion
/// Maps to: DELETE /api/auth/account
/// </summary>
public sealed class DeleteAccountRequest {
	[Required(ErrorMessage = "Password is required for account deletion")]
	public string Password { get; set; } = string.Empty;

	[Required(ErrorMessage = "Confirmation is required")]
	[RegularExpression("DELETE", ErrorMessage = "Confirmation must be 'DELETE'")]
	public string Confirmation { get; set; } = string.Empty;
}

