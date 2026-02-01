using Microsoft.Playwright;

namespace _10xCards.E2ETests.Infrastructure;

/// <summary>
/// Manages Playwright browser lifecycle for E2E tests
/// Provides browser and page instances for test execution
/// </summary>
public class PlaywrightFixture : IAsyncLifetime {
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IBrowserContext? BrowserContext { get; private set; }
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Initialize Playwright and launch browser
    /// Called once before tests
    /// Note: Browser installation is now handled by E2ETestCollectionFixture
    /// </summary>
    public async Task InitializeAsync() {
        _playwright = await Playwright.CreateAsync();

        // Launch browser in headless mode for CI/CD compatibility
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = true,
            Args = new[] { "--disable-dev-shm-usage" }
        });
    }

    /// <summary>
    /// Create a new browser context for test isolation
    /// Each test should get its own context
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync() {
        if (_browser == null)
            throw new InvalidOperationException("Browser not initialized");

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        return context;
    }

    /// <summary>
    /// Create a new page in a given context
    /// </summary>
    public async Task<IPage> CreatePageAsync(IBrowserContext context) {
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Clean up Playwright resources
    /// </summary>
    public async Task DisposeAsync() {
        if (BrowserContext != null) {
            await BrowserContext.DisposeAsync();
        }

        if (_browser != null) {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}

