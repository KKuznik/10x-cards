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
		} catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued")) {
			// Expected during server-side rendering (SSR) - JavaScript is not available yet
			_logger.LogDebug("JavaScript interop not available (SSR): {Message}", ex.Message);
			return false;
		} catch (JSException ex) {
			// JavaScript error occurred (e.g., function not found)
			_logger.LogWarning(ex, "JavaScript error checking authentication status");
			return false;
		} catch (JSDisconnectedException ex) {
			// JavaScript runtime disconnected (e.g., browser closed)
			_logger.LogDebug(ex, "JavaScript runtime disconnected");
			return false;
		} catch (Exception ex) {
			_logger.LogError(ex, "Unexpected error checking authentication status");
			return false;
		}
	}

	/// <summary>
	/// Retrieves the authenticated user's username from browser storage
	/// </summary>
	public async Task<string?> GetUsernameAsync() {
		try {
			return await _jsRuntime.InvokeAsync<string?>("getUsername");
		} catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued")) {
			// Expected during server-side rendering (SSR) - JavaScript is not available yet
			_logger.LogDebug("JavaScript interop not available (SSR): {Message}", ex.Message);
			return null;
		} catch (JSException ex) {
			// JavaScript error occurred (e.g., function not found)
			_logger.LogWarning(ex, "JavaScript error getting username");
			return null;
		} catch (JSDisconnectedException ex) {
			// JavaScript runtime disconnected (e.g., browser closed)
			_logger.LogDebug(ex, "JavaScript runtime disconnected");
			return null;
		} catch (Exception ex) {
			_logger.LogError(ex, "Unexpected error getting username");
			return null;
		}
	}

	/// <summary>
	/// Retrieves the JWT authentication token from browser storage
	/// </summary>
	public async Task<string?> GetAuthTokenAsync() {
		try {
			return await _jsRuntime.InvokeAsync<string?>("getAuthToken");
		} catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued")) {
			// Expected during server-side rendering (SSR) - JavaScript is not available yet
			_logger.LogDebug("JavaScript interop not available (SSR): {Message}", ex.Message);
			return null;
		} catch (JSException ex) {
			// JavaScript error occurred (e.g., function not found)
			_logger.LogWarning(ex, "JavaScript error getting auth token");
			return null;
		} catch (JSDisconnectedException ex) {
			// JavaScript runtime disconnected (e.g., browser closed)
			_logger.LogDebug(ex, "JavaScript runtime disconnected");
			return null;
		} catch (Exception ex) {
			_logger.LogError(ex, "Unexpected error getting auth token");
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
		} catch (JSException ex) {
			// JavaScript function not found or error - this can happen during SSR or if auth.js isn't loaded yet
			// Log but don't throw - authentication will be re-established after navigation/page load
			_logger.LogWarning(ex, "JavaScript error saving authentication data - auth.js may not be ready yet");
		} catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop")) {
			// JavaScript interop not available (SSR) - this is expected
			_logger.LogDebug("JavaScript interop not available during SSR - authentication will be established on client");
		} catch (Exception ex) {
			// Log other unexpected errors but don't throw - allow navigation to proceed
			_logger.LogError(ex, "Unexpected error saving authentication data");
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
		} catch (Exception ex) {
			_logger.LogError(ex, "Failed to clear authentication data");
			throw;
		}
	}
}

