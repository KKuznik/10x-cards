using _10xCards.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;

namespace _10xCards.Tests.Services;

public class ClientAuthenticationServiceTests {
	private readonly IJSRuntime _jsRuntime;
	private readonly ClientAuthenticationStateProvider _authStateProvider;
	private readonly ILogger<ClientAuthenticationService> _logger;
	private readonly ClientAuthenticationService _service;

	public ClientAuthenticationServiceTests() {
		_jsRuntime = Substitute.For<IJSRuntime>();
		var authProviderJsRuntime = Substitute.For<IJSRuntime>();
		var authProviderLogger = Substitute.For<ILogger<ClientAuthenticationStateProvider>>();
		_authStateProvider = Substitute.ForPartsOf<ClientAuthenticationStateProvider>(authProviderJsRuntime, authProviderLogger);
		_logger = Substitute.For<ILogger<ClientAuthenticationService>>();

		_service = new ClientAuthenticationService(_jsRuntime, _authStateProvider, _logger);
	}

	#region IsAuthenticatedAsync Tests

	[Fact]
	public async Task IsAuthenticatedAsync_WhenAuthenticated_ReturnsTrue() {
		// Arrange
		_jsRuntime.InvokeAsync<bool>("isAuthenticated", Arg.Any<object[]>())
			.Returns(true);

		// Act
		var result = await _service.IsAuthenticatedAsync();

		// Assert
		result.Should().BeTrue();
		await _jsRuntime.Received(1).InvokeAsync<bool>("isAuthenticated", Arg.Any<object[]>());
	}

	[Fact]
	public async Task IsAuthenticatedAsync_WhenNotAuthenticated_ReturnsFalse() {
		// Arrange
		_jsRuntime.InvokeAsync<bool>("isAuthenticated", Arg.Any<object[]>())
			.Returns(false);

		// Act
		var result = await _service.IsAuthenticatedAsync();

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task IsAuthenticatedAsync_WhenJSRuntimeThrows_ReturnsFalse() {
		// Arrange
		_jsRuntime.InvokeAsync<bool>("isAuthenticated", Arg.Any<object[]>())
			.Returns<bool>(x => throw new JSException("JS error"));

		// Act
		var result = await _service.IsAuthenticatedAsync();

		// Assert
		result.Should().BeFalse();
	}

	#endregion

	#region GetUsernameAsync Tests

	[Fact]
	public async Task GetUsernameAsync_WhenUsernameExists_ReturnsUsername() {
		// Arrange
		var expectedUsername = "test@example.com";
		_jsRuntime.InvokeAsync<string?>("getUsername", Arg.Any<object[]>())
			.Returns(expectedUsername);

		// Act
		var result = await _service.GetUsernameAsync();

		// Assert
		result.Should().Be(expectedUsername);
		await _jsRuntime.Received(1).InvokeAsync<string?>("getUsername", Arg.Any<object[]>());
	}

	[Fact]
	public async Task GetUsernameAsync_WhenNoUsername_ReturnsNull() {
		// Arrange
		_jsRuntime.InvokeAsync<string?>("getUsername", Arg.Any<object[]>())
			.Returns((string?)null);

		// Act
		var result = await _service.GetUsernameAsync();

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetUsernameAsync_WhenJSRuntimeThrows_ReturnsNull() {
		// Arrange
		_jsRuntime.InvokeAsync<string?>("getUsername", Arg.Any<object[]>())
			.Returns<string?>(x => throw new JSException("JS error"));

		// Act
		var result = await _service.GetUsernameAsync();

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region GetAuthTokenAsync Tests

	[Fact]
	public async Task GetAuthTokenAsync_WhenTokenExists_ReturnsToken() {
		// Arrange
		var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";
		_jsRuntime.InvokeAsync<string?>("getAuthToken", Arg.Any<object[]>())
			.Returns(expectedToken);

		// Act
		var result = await _service.GetAuthTokenAsync();

		// Assert
		result.Should().Be(expectedToken);
		await _jsRuntime.Received(1).InvokeAsync<string?>("getAuthToken", Arg.Any<object[]>());
	}

	[Fact]
	public async Task GetAuthTokenAsync_WhenNoToken_ReturnsNull() {
		// Arrange
		_jsRuntime.InvokeAsync<string?>("getAuthToken", Arg.Any<object[]>())
			.Returns((string?)null);

		// Act
		var result = await _service.GetAuthTokenAsync();

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetAuthTokenAsync_WhenJSRuntimeThrows_ReturnsNull() {
		// Arrange
		_jsRuntime.InvokeAsync<string?>("getAuthToken", Arg.Any<object[]>())
			.Returns<string?>(x => throw new JSException("JS error"));

		// Act
		var result = await _service.GetAuthTokenAsync();

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region LoginAsync Tests

	[Fact]
	public async Task LoginAsync_SavesTokenAndUsername() {
		// Arrange
		var token = "test-jwt-token";
		var expiresAt = DateTime.UtcNow.AddHours(1);
		var email = "user@example.com";

		// Act
		await _service.LoginAsync(token, expiresAt, email);

		// Assert
		await _jsRuntime.Received(1).InvokeVoidAsync("saveAuthToken",
			Arg.Is<object[]>(args =>
				args.Length == 2 &&
				args[0].ToString() == token));
		await _jsRuntime.Received(1).InvokeVoidAsync("saveUsername",
			Arg.Is<object[]>(args =>
				args.Length == 1 &&
				args[0].ToString() == email));
	}

	[Fact]
	public async Task LoginAsync_NotifiesAuthenticationStateChanged() {
		// Arrange
		var token = "test-jwt-token";
		var expiresAt = DateTime.UtcNow.AddHours(1);
		var email = "user@example.com";

		// Act
		await _service.LoginAsync(token, expiresAt, email);

		// Assert
		_authStateProvider.Received(1).NotifyAuthenticationStateChanged();
	}

	#endregion

	#region LogoutAsync Tests

	[Fact]
	public async Task LogoutAsync_ClearsAuthToken() {
		// Arrange
		// No setup needed - NSubstitute will return default ValueTask

		// Act
		await _service.LogoutAsync();

		// Assert
		await _jsRuntime.Received(1).InvokeVoidAsync("clearAuthToken", Arg.Any<object[]>());
	}

	[Fact]
	public async Task LogoutAsync_NotifiesAuthenticationStateChanged() {
		// Arrange
		// No setup needed - NSubstitute will return default ValueTask

		// Act
		await _service.LogoutAsync();

		// Assert
		_authStateProvider.Received(1).NotifyAuthenticationStateChanged();
	}

	[Fact]
	public async Task LogoutAsync_WhenJSRuntimeThrows_ThrowsException() {
		// Arrange
		_jsRuntime.When(x => x.InvokeVoidAsync("clearAuthToken", Arg.Any<object[]>()))
			.Do(x => throw new JSException("JS error"));

		// Act & Assert
		await Assert.ThrowsAsync<JSException>(async () =>
			await _service.LogoutAsync());
	}

	#endregion
}

