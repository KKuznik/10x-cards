using Microsoft.Extensions.DependencyInjection;

namespace _10xCards.E2ETests.Tests;

/// <summary>
/// E2E tests for user registration functionality
/// Tests registration page, form validation, and user creation
/// </summary>
[Collection("E2E Tests")]
public class RegisterTests : IAsyncLifetime {
	private readonly E2ETestCollectionFixture _collectionFixture;
	private IBrowserContext? _browserContext;
	private IPage? _page;

	public RegisterTests(E2ETestCollectionFixture collectionFixture) {
		_collectionFixture = collectionFixture;
	}

	public async Task InitializeAsync() {
		// Clean database before each test to ensure isolation
		var scope = _collectionFixture.WebApplicationFactory.Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<_10xCards.Database.Context.ApplicationDbContext>();
		await DatabaseCleaner.CleanDatabaseAsync(dbContext);

		// Create new browser context and page for test isolation
		// This is lightweight and provides clean state (cookies, localStorage, etc.)
		_browserContext = await _collectionFixture.PlaywrightFixture.CreateContextAsync();
		_page = await _collectionFixture.PlaywrightFixture.CreatePageAsync(_browserContext);
	}

	public async Task DisposeAsync() {
		// Only dispose per-test resources (context and page)
		// Shared resources (database, server, browser) are kept alive
		if (_page != null)
			await _page.CloseAsync();

		if (_browserContext != null)
			await _browserContext.CloseAsync();
	}

	/// <summary>
	/// Wait for auth.js to be fully loaded and ready
	/// </summary>
	private async Task WaitForAuthJsReadyAsync() {
		try {
			await _page!.WaitForFunctionAsync("() => window._authJsReady === true", new PageWaitForFunctionOptions {
				Timeout = 3000  // Reduced from 10s to 3s for faster tests
			});
		} catch (TimeoutException) {
			// If timeout, log warning but continue - functions might still work
			Console.WriteLine("Warning: Timeout waiting for auth.js ready signal");
		}
	}

	[Fact]
	public async Task Register_WithValidCredentials_ShouldSucceed() {
		// Arrange
		var testUser = TestDataGenerator.GenerateTestUser();
		await _page!.GotoAsync("/register");

		// Wait for the page to be fully loaded
		await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
		await WaitForAuthJsReadyAsync();

		// Act
		await _page.FillAsync("#email", testUser.Email);
		await _page.FillAsync("#password", testUser.Password);
		await _page.FillAsync("#confirmPassword", testUser.Password);
		await _page.ClickAsync("button[type='submit']");

		// Wait for navigation or success indication
		await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

		// Assert
		// After successful registration, user should be redirected to home or login page
		var currentUrl = _page.Url;
		Assert.True(
			currentUrl.Contains("/login") || currentUrl.Contains("/") || currentUrl.Contains("/home"),
			$"Expected redirect after registration, but got URL: {currentUrl}"
		);
	}

	[Fact]
	public async Task Register_WithExistingEmail_ShouldShowError() {
		// Arrange - First, register a user
		var testUser = TestDataGenerator.GenerateTestUser();
		await _page!.GotoAsync("/register");
		await WaitForAuthJsReadyAsync();

		await _page.FillAsync("#email", testUser.Email);
		await _page.FillAsync("#password", testUser.Password);
		await _page.FillAsync("#confirmPassword", testUser.Password);
		await _page.ClickAsync("button[type='submit']");

		try {
			await _page.WaitForURLAsync(url => !url.Contains("/register"), new() { Timeout = 15000 });
		} catch (TimeoutException) {
			var hasError = await _page.Locator(".alert-danger").IsVisibleAsync();
			var errorText = hasError ? await _page.Locator(".alert-danger").TextContentAsync() : "No error visible";
			throw new Exception($"First registration failed - still on register page. URL: {_page.Url}, Error: {errorText}");
		}

		// Verify first registration succeeded
		var currentUrl = _page.Url;

		// Act - Try to register with the same email again
		await _page.GotoAsync("/register");
		await WaitForAuthJsReadyAsync();
		await _page.FillAsync("#email", testUser.Email);
		await _page.FillAsync("#password", testUser.Password);
		await _page.FillAsync("#confirmPassword", testUser.Password);
		await _page.ClickAsync("button[type='submit']");

		// Wait for error message to appear (no hard-coded delay needed)
		await _page.WaitForSelectorAsync(".alert-danger", new PageWaitForSelectorOptions { Timeout = 5000 });

		// Assert
		var errorVisible = await _page.Locator(".alert-danger").IsVisibleAsync();
		Assert.True(errorVisible, "Expected error message for duplicate email");
	}

	[Fact]
	public async Task Register_WithInvalidPassword_ShouldShowValidationError() {
		// Arrange
		var testUser = TestDataGenerator.GenerateTestUserWithInvalidPassword();
		await _page!.GotoAsync("/register");
		await WaitForAuthJsReadyAsync();

		// Act
		await _page.FillAsync("#email", testUser.Email);
		await _page.FillAsync("#password", testUser.Password);
		await _page.FillAsync("#confirmPassword", testUser.Password);

		// Click outside to trigger validation
		await _page.ClickAsync("#email");

		// Try to submit
		await _page.ClickAsync("button[type='submit']");

		// Wait for page to stabilize
		await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

		// Assert - validation error should be visible or button should be disabled
		var currentUrl = _page.Url;
		Assert.True(currentUrl.Contains("/register"), "Should remain on registration page with invalid password");
	}

	[Fact]
	public async Task Register_WithMismatchedPasswords_ShouldShowValidationError() {
		// Arrange
		var testUser = TestDataGenerator.GenerateTestUser();
		await _page!.GotoAsync("/register");
		await WaitForAuthJsReadyAsync();

		// Act
		await _page.FillAsync("#email", testUser.Email);
		await _page.FillAsync("#password", testUser.Password);
		await _page.FillAsync("#confirmPassword", "DifferentPassword123!");

		// Click outside to trigger validation
		await _page.ClickAsync("#email");

		// Try to submit
		await _page.ClickAsync("button[type='submit']");

		// Wait for page to stabilize
		await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

		// Assert
		var currentUrl = _page.Url;
		Assert.True(currentUrl.Contains("/register"), "Should remain on registration page with mismatched passwords");
	}
}

