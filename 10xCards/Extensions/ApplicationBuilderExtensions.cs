using _10xCards.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Extensions;

public static class ApplicationBuilderExtensions {
    /// <summary>
    /// Applies any pending database migrations to the database.
    /// </summary>
    /// <param name="app">The application builder instance</param>
    /// <returns>The application builder instance for chaining</returns>
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app) {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try {
            var context = services.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("Starting database migration...");
            context.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");
        } catch (Exception ex) {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }

        return app;
    }
}

