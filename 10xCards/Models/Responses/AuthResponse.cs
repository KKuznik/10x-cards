namespace _10xCards.Models.Responses;

/// <summary>
/// Response model for authentication operations (register and login)
/// Maps to: POST /api/auth/register, POST /api/auth/login
/// Based on: User entity (IdentityUser)
/// </summary>
public sealed class AuthResponse {
	public Guid UserId { get; set; }
	public string Email { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
	public DateTime ExpiresAt { get; set; }
}

