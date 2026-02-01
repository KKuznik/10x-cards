using _10xCards.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.E2ETests.Infrastructure;

/// <summary>
/// Helper class for cleaning database between E2E tests
/// Removes all data from tables while preserving schema
/// </summary>
public static class DatabaseCleaner {
	/// <summary>
	/// Cleans all data from database tables
	/// Uses TRUNCATE for better performance compared to DELETE
	/// </summary>
	public static async Task CleanDatabaseAsync(ApplicationDbContext context) {
		// Start a transaction for atomic cleanup
		await using var transaction = await context.Database.BeginTransactionAsync();

		try {
			// Disable triggers temporarily to avoid foreign key constraint issues
			await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica'");

			// Use TRUNCATE for much faster cleanup
			// TRUNCATE is faster than DELETE as it doesn't scan tables or generate undo logs
			await context.Database.ExecuteSqlRawAsync(@"
                TRUNCATE TABLE flashcards, 
                             generations, 
                             generation_error_logs, 
                             user_tokens, 
                             user_logins, 
                             user_claims, 
                             user_roles, 
                             role_claims, 
                             users, 
                             roles 
                RESTART IDENTITY CASCADE
            ");

			// Re-enable triggers
			await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin'");

			await transaction.CommitAsync();
		} catch {
			// Re-enable triggers in case of error
			try {
				await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin'");
			} catch { /* Ignore errors during cleanup */ }

			await transaction.RollbackAsync();
			throw;
		}
	}
}

