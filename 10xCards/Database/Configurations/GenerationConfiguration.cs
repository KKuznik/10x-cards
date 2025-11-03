using _10xCards.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _10xCards.Database.Configurations;

public sealed class GenerationConfiguration : IEntityTypeConfiguration<Generation> {
	public void Configure(EntityTypeBuilder<Generation> builder) {
		builder.ToTable("generations");

		builder.HasKey(g => g.Id);

		builder.Property(g => g.Id)
			.HasColumnName("id")
			.ValueGeneratedOnAdd();

		builder.Property(g => g.UserId)
			.HasColumnName("user_id")
			.IsRequired();

		builder.Property(g => g.Model)
			.HasColumnName("model")
			.IsRequired();

		builder.Property(g => g.GeneratedCount)
			.HasColumnName("generated_count")
			.IsRequired();

		builder.Property(g => g.AcceptedUneditedCount)
			.HasColumnName("accepted_unedited_count");

		builder.Property(g => g.AcceptedEditedCount)
			.HasColumnName("accepted_edited_count");

		builder.Property(g => g.SourceTextHash)
			.HasColumnName("source_text_hash")
			.IsRequired();

		builder.Property(g => g.SourceTextLength)
			.HasColumnName("source_text_length")
			.IsRequired();

		builder.Property(g => g.GenerationDuration)
			.HasColumnName("generation_duration")
			.IsRequired();

		builder.Property(g => g.CreatedAt)
			.HasColumnName("created_at")
			.HasColumnType("timestamp with time zone")
			.IsRequired()
			.HasDefaultValueSql("now()");

		builder.Property(g => g.UpdatedAt)
			.HasColumnName("updated_at")
			.HasColumnType("timestamp with time zone")
			.IsRequired()
			.HasDefaultValueSql("now()");

		// CHECK constraint for source_text_length
		builder.ToTable(tb => tb.HasCheckConstraint(
			"CK_generations_source_text_length",
			"source_text_length BETWEEN 1000 AND 10000"
		));

		// Foreign key relationship
		builder.HasOne(g => g.User)
			.WithMany(u => u.Generations)
			.HasForeignKey(g => g.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// Index
		builder.HasIndex(g => g.UserId)
			.HasDatabaseName("IX_generations_user_id");
	}
}

