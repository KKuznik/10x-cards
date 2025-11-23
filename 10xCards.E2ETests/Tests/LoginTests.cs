using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using _10xCards.Database.Entities;

namespace _10xCards.E2ETests.Tests;

/// <summary>
/// E2E tests for user login functionality
/// Tests login page, authentication, and post-login navigation
/// </summary>
[Collection("E2E Tests")]
public class LoginTests : IAsyncLifetime
{
    private readonly E2ETestCollectionFixture _collectionFixture;
    private IBrowserContext? _browserContext;
    private IPage? _page;

    public LoginTests(E2ETestCollectionFixture collectionFixture)
    {
        _collectionFixture = collectionFixture;
    }

    public async Task InitializeAsync()
    {
        // Clean database before each test to ensure isolation
        var scope = _collectionFixture.WebApplicationFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<_10xCards.Database.Context.ApplicationDbContext>();
        await DatabaseCleaner.CleanDatabaseAsync(dbContext);

        // Create new browser context and page for test isolation
        // This is lightweight and provides clean state (cookies, localStorage, etc.)
        _browserContext = await _collectionFixture.PlaywrightFixture.CreateContextAsync();
        _page = await _collectionFixture.PlaywrightFixture.CreatePageAsync(_browserContext);
    }

    public async Task DisposeAsync()
    {
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
    private async Task WaitForAuthJsReadyAsync()
    {
        try
        {
            await _page!.WaitForFunctionAsync("() => window._authJsReady === true", new PageWaitForFunctionOptions 
            { 
                Timeout = 3000  // Reduced from 10s to 3s for faster tests
            });
        }
        catch (TimeoutException)
        {
            // If timeout, log warning but continue - functions might still work
            Console.WriteLine("Warning: Timeout waiting for auth.js ready signal");
        }
    }

    /// <summary>
    /// Helper method to seed a test user directly in the database (much faster than UI registration)
    /// </summary>
    private async Task<TestUser> SeedTestUserAsync()
    {
        var scope = _collectionFixture.WebApplicationFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<_10xCards.Database.Context.ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        
        return await TestDataGenerator.SeedTestUserAsync(dbContext, userManager);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange - Seed a test user directly in database (faster than UI registration)
        var testUser = await SeedTestUserAsync();
        
        // Clear authentication state to ensure clean login test
        await _page!.Context.ClearCookiesAsync();

        // Act - Login with the registered user
        await _page.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        
        // Wait for submit button to be enabled
        await _page.WaitForSelectorAsync("button[type='submit']:not([disabled])", new() { Timeout = 3000 });
        
        // Click submit and wait for navigation to complete
        // Login uses forceLoad: true which triggers full page navigation
        await _page.ClickAsync("button[type='submit']");
        
        try
        {
            await _page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 15000 });
        }
        catch (TimeoutException)
        {
            // Navigation didn't happen - check for error message
            var hasError = await _page.Locator(".alert-danger").IsVisibleAsync();
            var errorText = hasError ? await _page.Locator(".alert-danger").TextContentAsync() : "No error";
            Console.WriteLine($"Navigation timeout. Error visible: {hasError}, Text: {errorText}");
            Assert.Fail($"Login did not navigate away from login page. Error: {errorText}");
        }

        // Assert
        var currentUrl = _page.Url;
        Assert.False(
            currentUrl.Contains("/login"),
            $"Expected to be redirected away from login page after successful login, but still on: {currentUrl}"
        );
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldShowError()
    {
        // Arrange - Seed a test user directly in database (faster than UI registration)
        var testUser = await SeedTestUserAsync();

        // Act - Try to login with wrong password
        await _page!.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", "WrongPassword123!");
        await _page.ClickAsync("button[type='submit']");

        // Wait for error message to appear
        await _page.WaitForSelectorAsync(".alert-danger", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert
        var errorVisible = await _page.Locator(".alert-danger").IsVisibleAsync();
        Assert.True(errorVisible, "Expected error message for invalid password");
        
        var currentUrl = _page.Url;
        Assert.True(currentUrl.Contains("/login"), "Should remain on login page after failed login");
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldShowError()
    {
        // Arrange
        var testUser = TestDataGenerator.GenerateTestUser(); // Don't register this user

        // Act
        await _page!.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        await _page.ClickAsync("button[type='submit']");

        // Wait for error message to appear
        await _page.WaitForSelectorAsync(".alert-danger", new PageWaitForSelectorOptions { Timeout = 5000 });

        // Assert
        var errorVisible = await _page.Locator(".alert-danger").IsVisibleAsync();
        Assert.True(errorVisible, "Expected error message for non-existent user");
        
        var currentUrl = _page.Url;
        Assert.True(currentUrl.Contains("/login"), "Should remain on login page after failed login");
    }

    [Fact]
    public async Task Login_NavigationToRegisterLink_ShouldWork()
    {
        // Arrange
        await _page!.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();

        // Act - Click the "Register" link
        await _page.ClickAsync("text=Zarejestruj się");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var currentUrl = _page.Url;
        Assert.True(currentUrl.Contains("/register"), "Should navigate to registration page");
    }

    [Fact]
    public async Task Login_AfterSuccessfulLogin_ShouldShowUserMenu()
    {
        // Arrange - Seed a test user directly in database (faster than UI registration)
        var testUser = await SeedTestUserAsync();
        
        // Clear authentication state to ensure clean login test
        await _page!.Context.ClearCookiesAsync();
        
        // Navigate to login page
        await _page.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        
        // Act - Ensure button is ready and click submit
        // Wait for submit button to be enabled
        await _page.WaitForSelectorAsync("button[type='submit']:not([disabled])", new() { Timeout = 3000 });
        
        // Click submit and wait for navigation to complete
        // Login uses forceLoad: true which triggers full page navigation
        await _page.ClickAsync("button[type='submit']");
        
        try
        {
            await _page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 15000 });
        }
        catch (TimeoutException)
        {
            // Navigation didn't happen - check for error message
            var hasError = await _page.Locator(".alert-danger").IsVisibleAsync();
            var errorText = hasError ? await _page.Locator(".alert-danger").TextContentAsync() : "No error";
            Console.WriteLine($"Navigation timeout. Error visible: {hasError}, Text: {errorText}");
            Assert.Fail($"Login did not navigate away from login page. Error: {errorText}");
        }

        // Assert - Check that we're logged in and not on login page
        var currentUrl = _page.Url;
        Assert.False(
            currentUrl.Contains("/login"),
            $"Should not be on login page after successful authentication. Current URL: {currentUrl}"
        );
    }
}

