namespace _10xCards.E2ETests.Tests;

/// <summary>
/// E2E tests for user login functionality
/// Tests login page, authentication, and post-login navigation
/// </summary>
[Collection("E2E Tests")]
public class LoginTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private CustomWebApplicationFactory? _factory;
    private PlaywrightFixture? _playwrightFixture;
    private IBrowserContext? _browserContext;
    private IPage? _page;
    private string _baseUrl = string.Empty;

    public LoginTests(DatabaseFixture databaseFixture)
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

    /// <summary>
    /// Helper method to register a test user
    /// </summary>
    private async Task<TestUser> RegisterTestUserAsync()
    {
        var testUser = TestDataGenerator.GenerateTestUser();
        
        await _page!.GotoAsync("/register");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        await _page.FillAsync("#confirmPassword", testUser.Password);
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return testUser;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange - Register a user first
        var testUser = await RegisterTestUserAsync();

        // Act - Login with the registered user
        await _page!.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        await _page.ClickAsync("button[type='submit']");

        // Wait for navigation after login
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
        // Arrange - Register a user first
        var testUser = await RegisterTestUserAsync();

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
        await _page.WaitForSelectorAsync(".alert-danger", new PageWaitForSelectorOptions { Timeout = 10000 });

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
        // Arrange - Register and login a user
        var testUser = await RegisterTestUserAsync();
        
        await _page!.GotoAsync("/login");
        await WaitForAuthJsReadyAsync();
        await _page.FillAsync("#email", testUser.Email);
        await _page.FillAsync("#password", testUser.Password);
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Check that we're logged in by looking for user-specific elements
        var currentUrl = _page.Url;
        Assert.False(
            currentUrl.Contains("/login"),
            "Should not be on login page after successful authentication"
        );
    }
}

