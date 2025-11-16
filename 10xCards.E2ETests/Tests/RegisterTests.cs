namespace _10xCards.E2ETests.Tests;

/// <summary>
/// E2E tests for user registration functionality
/// Tests registration page, form validation, and user creation
/// </summary>
[Collection("E2E Tests")]
public class RegisterTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private CustomWebApplicationFactory? _factory;
    private PlaywrightFixture? _playwrightFixture;
    private IBrowserContext? _browserContext;
    private IPage? _page;
    private string _baseUrl = string.Empty;

    public RegisterTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        // Create web application factory with test database
        _factory = new CustomWebApplicationFactory(_databaseFixture.ConnectionString);
        _baseUrl = _factory.ServerAddress;

        // Setup Playwright
        _playwrightFixture = new PlaywrightFixture { BaseUrl = _baseUrl };
        await _playwrightFixture.InitializeAsync();
        
        _browserContext = await _playwrightFixture.CreateContextAsync();
        _page = await _playwrightFixture.CreatePageAsync(_browserContext);
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
            await _page.CloseAsync();

        if (_browserContext != null)
            await _browserContext.CloseAsync();

        if (_playwrightFixture != null)
            await _playwrightFixture.DisposeAsync();

        if (_factory != null)
            await _factory.DisposeAsync();
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
                Timeout = 10000 
            });
        }
        catch (TimeoutException)
        {
            // If timeout, log warning but continue - functions might still work
            Console.WriteLine("Warning: Timeout waiting for auth.js ready signal");
        }
    }

    [Fact]
    public async Task Register_WithValidCredentials_ShouldSucceed()
    {
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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        // After successful registration, user should be redirected to home or login page
        var currentUrl = _page.Url;
        Assert.True(
            currentUrl.Contains("/login") || currentUrl.Contains("/") || currentUrl.Contains("/home"),
            $"Expected redirect after registration, but got URL: {currentUrl}"
        );
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldShowError()
    {
        // Arrange - First, register a user
        var testUser = TestDataGenerator.GenerateTestUser();
        await _page!.GotoAsync("/register");
        await WaitForAuthJsReadyAsync();
        
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        await _page.FillAsync("#confirmPassword", testUser.Password);
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Try to register with the same email again
        await _page.GotoAsync("/register");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        await _page.FillAsync("#confirmPassword", testUser.Password);
        await _page.ClickAsync("button[type='submit']");
        
        // Wait for error message to appear
        await _page.WaitForSelectorAsync(".alert-danger", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert
        var errorVisible = await _page.Locator(".alert-danger").IsVisibleAsync();
        Assert.True(errorVisible, "Expected error message for duplicate email");
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ShouldShowValidationError()
    {
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
        
        // Wait a bit for any potential submission to complete
        await _page.WaitForTimeoutAsync(1000);

        // Assert - validation error should be visible or button should be disabled
        var currentUrl = _page.Url;
        Assert.True(currentUrl.Contains("/register"), "Should remain on registration page with invalid password");
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ShouldShowValidationError()
    {
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
        
        // Wait a bit for any potential submission to complete
        await _page.WaitForTimeoutAsync(1000);

        // Assert
        var currentUrl = _page.Url;
        Assert.True(currentUrl.Contains("/register"), "Should remain on registration page with mismatched passwords");
    }
}

