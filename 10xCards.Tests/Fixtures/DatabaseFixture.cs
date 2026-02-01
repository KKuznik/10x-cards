using _10xCards.Database.Context;
using _10xCards.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Tests.Fixtures;

/// <summary>
/// Test fixture for managing in-memory database across multiple tests
/// Implements IDisposable for proper cleanup after tests
/// </summary>
public class DatabaseFixture : IDisposable {
	public ApplicationDbContext Context { get; private set; }
	private bool _disposed = false;

	public DatabaseFixture() {
		// Create unique database name for each test run to avoid conflicts
		var databaseName = $"TestDatabase_{Guid.NewGuid()}";

		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName)
			.Options;

		Context = new ApplicationDbContext(options);

		// Ensure database is created
		Context.Database.EnsureCreated();
	}

	/// <summary>
	/// Seeds the database with test data
	/// </summary>
	public void SeedDatabase() {
		// Clear existing data first
		Context.Flashcards.RemoveRange(Context.Flashcards);
		Context.Generations.RemoveRange(Context.Generations);
		Context.GenerationErrorLogs.RemoveRange(Context.GenerationErrorLogs);
		Context.SaveChanges();

		// Check if users already exist
		var user1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
		var user2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

		var existingUser1 = Context.Users.Find(user1Id);
		var existingUser2 = Context.Users.Find(user2Id);

		// Only add users if they don't exist
		if (existingUser1 == null) {
			var user1 = new User {
				Id = user1Id,
				UserName = "test1@example.com",
				Email = "test1@example.com",
				NormalizedUserName = "TEST1@EXAMPLE.COM",
				NormalizedEmail = "TEST1@EXAMPLE.COM",
				EmailConfirmed = true
			};
			Context.Users.Add(user1);
		}

		if (existingUser2 == null) {
			var user2 = new User {
				Id = user2Id,
				UserName = "test2@example.com",
				Email = "test2@example.com",
				NormalizedUserName = "TEST2@EXAMPLE.COM",
				NormalizedEmail = "TEST2@EXAMPLE.COM",
				EmailConfirmed = true
			};
			Context.Users.Add(user2);
		}

		Context.SaveChanges();

		// Detach all tracked entities to avoid conflicts in tests
		Context.ChangeTracker.Clear();
	}

	/// <summary>
	/// Clears all data from the database
	/// </summary>
	public void ClearDatabase() {
		Context.Flashcards.RemoveRange(Context.Flashcards);
		Context.Generations.RemoveRange(Context.Generations);
		Context.GenerationErrorLogs.RemoveRange(Context.GenerationErrorLogs);
		Context.Users.RemoveRange(Context.Users);
		Context.SaveChanges();
	}

	/// <summary>
	/// Creates a new isolated context for testing
	/// Useful when you need a fresh context instance
	/// </summary>
	public ApplicationDbContext CreateNewContext() {
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Context.Database.GetDbConnection().Database)
			.Options;

		return new ApplicationDbContext(options);
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing) {
				Context.Database.EnsureDeleted();
				Context.Dispose();
			}
			_disposed = true;
		}
	}
}

