using _10xCards.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _10xCards.Database.Configurations;

public sealed class FlashcardConfiguration : IEntityTypeConfiguration<Flashcard> {
    public void Configure(EntityTypeBuilder<Flashcard> builder) {
        builder.ToTable("flashcards");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(f => f.Front)
            .HasColumnName("front")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Back)
            .HasColumnName("back")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.Source)
            .HasColumnName("source")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(f => f.GenerationId)
            .HasColumnName("generation_id");

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // CHECK constraint for source
        builder.ToTable(tb => tb.HasCheckConstraint(
            "CK_flashcards_source",
            "source IN ('ai-full', 'ai-edited', 'manual')"
        ));

        // Foreign key relationships
        builder.HasOne(f => f.User)
            .WithMany(u => u.Flashcards)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Generation)
            .WithMany(g => g.Flashcards)
            .HasForeignKey(f => f.GenerationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("IX_flashcards_user_id");

        builder.HasIndex(f => f.GenerationId)
            .HasDatabaseName("IX_flashcards_generation_id");
    }
}

