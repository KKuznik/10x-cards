using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;

namespace _10xCards.Services;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthService {
	/// <summary>
	/// Registers a new user in the system
	/// </summary>
	/// <param name="request">Registration request containing email and password</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing AuthResponse with JWT token on success, or error details on failure</returns>
	Task<Result<AuthResponse>> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Authenticates an existing user
	/// </summary>
	/// <param name="request">Login request containing email and password</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing AuthResponse with JWT token on success, or error details on failure</returns>
	Task<Result<AuthResponse>> LoginUserAsync(LoginRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Logs out the current user by invalidating their session
	/// </summary>
	/// <param name="userId">ID of the user to log out</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result<bool>> LogoutUserAsync(
		Guid userId,
		CancellationToken cancellationToken = default);
}

