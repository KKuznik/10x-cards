namespace _10xCards.Services;

/// <summary>
/// Service interface for client-side authentication state management
/// Manages authentication state in browser localStorage via JavaScript interop
/// </summary>
public interface IClientAuthenticationService {
	/// <summary>
	/// Checks if the user is currently authenticated with a valid non-expired token
	/// </summary>
	/// <returns>True if authenticated, false otherwise</returns>
	Task<bool> IsAuthenticatedAsync();

	/// <summary>
	/// Retrieves the authenticated user's username from browser storage
	/// </summary>
	/// <returns>Username if authenticated, null otherwise</returns>
	Task<string?> GetUsernameAsync();

	/// <summary>
	/// Retrieves the JWT authentication token from browser storage
	/// </summary>
	/// <returns>JWT token if exists, null otherwise</returns>
	Task<string?> GetAuthTokenAsync();

	/// <summary>
	/// Saves authentication data to browser storage and notifies authentication state change
	/// </summary>
	/// <param name="token">JWT authentication token</param>
	/// <param name="expiresAt">Token expiration date and time</param>
	/// <param name="email">User's email address</param>
	Task LoginAsync(string token, DateTime expiresAt, string email);

	/// <summary>
	/// Clears authentication data from browser storage and notifies authentication state change
	/// </summary>
	Task LogoutAsync();
}

