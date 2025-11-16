using Testcontainers.PostgreSql;

namespace _10xCards.E2ETests.Infrastructure;

/// <summary>
/// Manages PostgreSQL Testcontainer lifecycle for E2E tests
/// Shared across all tests for performance
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Initialize and start the PostgreSQL container
    /// Called once before all tests
    /// </summary>
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("10xCards_Test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        
        ConnectionString = _container.GetConnectionString();
    }

    /// <summary>
    /// Stop and dispose the PostgreSQL container
    /// Called once after all tests complete
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

