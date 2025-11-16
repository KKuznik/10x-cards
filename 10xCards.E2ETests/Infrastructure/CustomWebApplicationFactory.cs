using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using _10xCards.Database.Context;

namespace _10xCards.E2ETests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for E2E testing
/// Configures the application to use test database and settings
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private IHost? _host;

    public string ServerAddress
    {
        get
        {
            EnsureServer();
            return _serverAddress;
        }
    }
    private string _serverAddress = string.Empty;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
        
        // Set environment variables for test configuration
        Environment.SetEnvironmentVariable("ConnectionStrings__Database", _connectionString);
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "ThisIsAVerySecureTestSecretKeyWith64CharactersForSecurityToken!");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "10xCards");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "10xCards");
        Environment.SetEnvironmentVariable("JwtSettings__ExpirationInMinutes", "480");
        Environment.SetEnvironmentVariable("OpenRouter__ApiKey", "test-api-key");
        Environment.SetEnvironmentVariable("OpenAI__ApiKey", "test-api-key");
    }

    private void EnsureServer()
    {
        if (string.IsNullOrEmpty(_serverAddress))
        {
            // Trigger server creation by accessing the Server property
            _ = Server;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration sources to ensure our test config takes precedence
            config.Sources.Clear();
            
            // Add test configuration
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _connectionString,
                ["JwtSettings:SecretKey"] = "ThisIsAVerySecureTestSecretKeyWith64CharactersForSecurityToken!",
                ["JwtSettings:Issuer"] = "10xCards",
                ["JwtSettings:Audience"] = "10xCards",
                ["JwtSettings:ExpirationInMinutes"] = "480",
                ["OpenRouter:ApiKey"] = "test-api-key",
                ["OpenAI:ApiKey"] = "test-api-key"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test database connection
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_connectionString)
                       .UseSnakeCaseNamingConvention();
            });

            // Build service provider and ensure database is created with migrations
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
        // Use Kestrel with a dynamic port
        builder.UseKestrel(options =>
        {
            options.ListenAnyIP(0); // 0 = dynamic port
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create a test host with a real server
        var testHost = builder.Build();
        
        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel(options =>
        {
            options.ListenAnyIP(0);
        }));

        _host = builder.Build();
        _host.Start();

        // Get the actual server address
        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var serverAddress = addresses!.Addresses.FirstOrDefault() ?? "http://localhost:5000";
        
        // Convert IPv6 address to localhost URL for Playwright compatibility
        if (serverAddress.Contains("[::]:"))
        {
            var uri = new Uri(serverAddress);
            _serverAddress = $"http://localhost:{uri.Port}";
        }
        else
        {
            _serverAddress = serverAddress;
        }

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _host?.Dispose();
        base.Dispose(disposing);
    }
}

