using _10xCards.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Database.Context;

public sealed class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid> {

	public DbSet<Flashcard> Flashcards => Set<Flashcard>();
	public DbSet<Generation> Generations => Set<Generation>();
	public DbSet<GenerationErrorLog> GenerationErrorLogs => Set<GenerationErrorLog>();

	public ApplicationDbContext(DbContextOptions options)
		: base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

		base.OnModelCreating(modelBuilder);

		// Zmiana nazw tabel Identity na snake_case z małymi literami
		modelBuilder.Entity<User>().ToTable("users");
		modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
		modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
		modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
		modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
		modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
		modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
	}
}
