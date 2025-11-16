using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace _10xCards.Services;

/// <summary>
/// Custom authentication state provider that reads JWT token from browser localStorage
/// and creates claims principal for Blazor authentication
/// </summary>
public class ClientAuthenticationStateProvider : AuthenticationStateProvider {
	private readonly IJSRuntime _jsRuntime;
	private readonly ILogger<ClientAuthenticationStateProvider> _logger;

	public ClientAuthenticationStateProvider(
		IJSRuntime jsRuntime,
		ILogger<ClientAuthenticationStateProvider> logger) {
		_jsRuntime = jsRuntime;
		_logger = logger;
	}

	/// <summary>
	/// Gets the current authentication state by reading and parsing the JWT token from localStorage
	/// </summary>
	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		try {
			// Get token from localStorage
			var token = await _jsRuntime.InvokeAsync<string?>("getAuthToken");

			// Early return: no token found
			if (string.IsNullOrWhiteSpace(token)) {
				return CreateAnonymousState();
			}

			// Check if token is expired
			var isExpired = await _jsRuntime.InvokeAsync<bool>("isTokenExpired");
			if (isExpired) {
				_logger.LogDebug("Token is expired");
				return CreateAnonymousState();
			}

			// Parse JWT token and extract claims
			var claims = ParseClaimsFromJwt(token);
			if (claims == null || !claims.Any()) {
				_logger.LogWarning("Failed to parse claims from token");
				return CreateAnonymousState();
			}

			// Create authenticated user identity
			var identity = new ClaimsIdentity(claims, "jwt");
			var user = new ClaimsPrincipal(identity);

			return new AuthenticationState(user);
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued")) {
			// Expected during server-side rendering (SSR) - JavaScript is not available yet
			_logger.LogDebug("JavaScript interop not available (SSR): {Message}", ex.Message);
			return CreateAnonymousState();
		}
		catch (JSException ex) {
			// JavaScript error occurred (e.g., function not found)
			_logger.LogWarning(ex, "JavaScript error getting authentication state");
			return CreateAnonymousState();
		}
		catch (JSDisconnectedException ex) {
			// JavaScript runtime disconnected (e.g., browser closed)
			_logger.LogDebug(ex, "JavaScript runtime disconnected");
			return CreateAnonymousState();
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Unexpected error getting authentication state");
			return CreateAnonymousState();
		}
	}

	/// <summary>
	/// Notifies the authentication state has changed
	/// Should be called after login or logout operations
	/// </summary>
	public void NotifyAuthenticationStateChanged() {
		NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
	}

	/// <summary>
	/// Parses claims from a JWT token
	/// </summary>
	/// <param name="token">JWT token string</param>
	/// <returns>List of claims extracted from the token</returns>
	private List<Claim>? ParseClaimsFromJwt(string token) {
		try {
			var handler = new JwtSecurityTokenHandler();

			// Check if token can be read
			if (!handler.CanReadToken(token)) {
				_logger.LogWarning("Token cannot be read");
				return null;
			}

			var jwtToken = handler.ReadJwtToken(token);

			// Extract all claims from the token
			var claims = jwtToken.Claims.ToList();

			// Log for debugging (remove in production)
			_logger.LogDebug("Parsed {ClaimCount} claims from JWT token", claims.Count);

			return claims;
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Error parsing JWT token");
			return null;
		}
	}

	/// <summary>
	/// Creates an anonymous authentication state (not authenticated)
	/// </summary>
	private static AuthenticationState CreateAnonymousState() {
		var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
		return new AuthenticationState(anonymous);
	}
}

