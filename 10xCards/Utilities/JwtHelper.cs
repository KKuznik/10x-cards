using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace _10xCards.Utilities;

/// <summary>
/// Utility class for JWT token operations
/// </summary>
public static class JwtHelper {
	/// <summary>
	/// Extracts the user ID from a JWT token
	/// </summary>
	/// <param name="token">The JWT token string</param>
	/// <returns>The user ID as a Guid, or Guid.Empty if extraction fails</returns>
	public static Guid ExtractUserIdFromToken(string? token) {
		// Guard clause: validate token
		if (string.IsNullOrWhiteSpace(token)) {
			return Guid.Empty;
		}

		try {
			// Parse JWT token without validation (validation happens at API level)
			var handler = new JwtSecurityTokenHandler();
			
			// Check if token is valid JWT format
			if (!handler.CanReadToken(token)) {
				return Guid.Empty;
			}

			var jwtToken = handler.ReadJwtToken(token);

			// Try to get NameIdentifier claim (ClaimTypes.NameIdentifier)
			var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
				c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");

			// Early return: no claim found
			if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value)) {
				return Guid.Empty;
			}

			// Try to parse the claim value as Guid
			if (Guid.TryParse(userIdClaim.Value, out var userId)) {
				return userId;
			}

			return Guid.Empty;
		}
		catch {
			// If any error occurs during parsing, return empty Guid
			return Guid.Empty;
		}
	}
}

