using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace _10xCards.Services;

/// <summary>
/// Client-side authentication service that manages authentication state via JavaScript interop
/// </summary>
public sealed class ClientAuthenticationService : IClientAuthenticationService {
	private readonly IJSRuntime _jsRuntime;
	private readonly AuthenticationStateProvider _authenticationStateProvider;
	private readonly ILogger<ClientAuthenticationService> _logger;

	public ClientAuthenticationService(
		IJSRuntime jsRuntime,
		AuthenticationStateProvider authenticationStateProvider,
		ILogger<ClientAuthenticationService> logger) {
		_jsRuntime = jsRuntime;
		_authenticationStateProvider = authenticationStateProvider;
		_logger = logger;
	}

	/// <summary>
	/// Checks if the user is currently authenticated with a valid non-expired token
	/// </summary>
	public async Task<bool> IsAuthenticatedAsync() {
		try {
			return await _jsRuntime.InvokeAsync<bool>("isAuthenticated");
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Failed to check authentication status");
			return false;
		}
	}

	/// <summary>
	/// Retrieves the authenticated user's username from browser storage
	/// </summary>
	public async Task<string?> GetUsernameAsync() {
		try {
			return await _jsRuntime.InvokeAsync<string?>("getUsername");
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Failed to get username");
			return null;
		}
	}

	/// <summary>
	/// Retrieves the JWT authentication token from browser storage
	/// </summary>
	public async Task<string?> GetAuthTokenAsync() {
		try {
			return await _jsRuntime.InvokeAsync<string?>("getAuthToken");
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Failed to get auth token");
			return null;
		}
	}

	/// <summary>
	/// Saves authentication data to browser storage and notifies authentication state change
	/// </summary>
	public async Task LoginAsync(string token, DateTime expiresAt, string email) {
		try {
			// Save authentication data to localStorage
			await _jsRuntime.InvokeVoidAsync("saveAuthToken", token, expiresAt.ToString("O"));
			await _jsRuntime.InvokeVoidAsync("saveUsername", email);

			// Notify the authentication state provider that the user is now authenticated
			if (_authenticationStateProvider is ClientAuthenticationStateProvider provider) {
				provider.NotifyAuthenticationStateChanged();
			}
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Failed to save authentication data");
			throw;
		}
	}

	/// <summary>
	/// Clears authentication data from browser storage and notifies authentication state change
	/// </summary>
	public async Task LogoutAsync() {
		try {
			// Clear authentication data from localStorage
			await _jsRuntime.InvokeVoidAsync("clearAuthToken");

			// Notify the authentication state provider that the user is now logged out
			if (_authenticationStateProvider is ClientAuthenticationStateProvider provider) {
				provider.NotifyAuthenticationStateChanged();
			}
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Failed to clear authentication data");
			throw;
		}
	}
}

