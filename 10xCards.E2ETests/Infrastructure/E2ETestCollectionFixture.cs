namespace _10xCards.E2ETests.Infrastructure;

/// <summary>
/// Shared fixture for all E2E tests in the collection
/// Creates and maintains shared instances of database, web application, and Playwright browser
/// This significantly reduces test execution time by avoiding repeated initialization
/// </summary>
public class E2ETestCollectionFixture : IAsyncLifetime {
    private DatabaseFixture? _databaseFixture;
    private CustomWebApplicationFactory? _factory;
    private PlaywrightFixture? _playwrightFixture;

    public DatabaseFixture DatabaseFixture => _databaseFixture
        ?? throw new InvalidOperationException("DatabaseFixture not initialized");

    public CustomWebApplicationFactory WebApplicationFactory => _factory
        ?? throw new InvalidOperationException("WebApplicationFactory not initialized");

    public PlaywrightFixture PlaywrightFixture => _playwrightFixture
        ?? throw new InvalidOperationException("PlaywrightFixture not initialized");

    public string BaseUrl => WebApplicationFactory.ServerAddress;

    /// <summary>
    /// Initialize all shared resources once before any tests run
    /// </summary>
    public async Task InitializeAsync() {
        // 1. Start PostgreSQL container
        _databaseFixture = new DatabaseFixture();
        await _databaseFixture.InitializeAsync();

        // 2. Create web application factory with test database
        _factory = new CustomWebApplicationFactory(_databaseFixture.ConnectionString);

        // 3. Install Playwright browsers (only if not already installed)
        // Check if browsers are already installed to avoid unnecessary installation time
        try {
            // Try to verify browser installation by checking driver path
            var playwrightDriverPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright");

            var chromiumPath = Path.Combine(playwrightDriverPath, "chromium-*");
            var browserInstalled = Directory.Exists(playwrightDriverPath) && Directory.GetDirectories(Path.GetDirectoryName(chromiumPath) ?? playwrightDriverPath, "chromium-*").Length > 0;

            if (!browserInstalled) {
                Console.WriteLine("Installing Playwright browsers...");
                Microsoft.Playwright.Program.Main(new[] { "install", "chromium", "--with-deps" });
            }
        } catch {
            // If verification fails, install to be safe
            Console.WriteLine("Installing Playwright browsers...");
            Microsoft.Playwright.Program.Main(new[] { "install", "chromium", "--with-deps" });
        }

        // 4. Initialize Playwright and launch browser
        _playwrightFixture = new PlaywrightFixture { BaseUrl = BaseUrl };
        await _playwrightFixture.InitializeAsync();
    }

    /// <summary>
    /// Clean up all shared resources after all tests complete
    /// </summary>
    public async Task DisposeAsync() {
        if (_playwrightFixture != null)
            await _playwrightFixture.DisposeAsync();

        if (_factory != null)
            await _factory.DisposeAsync();

        if (_databaseFixture != null)
            await _databaseFixture.DisposeAsync();
    }
}

