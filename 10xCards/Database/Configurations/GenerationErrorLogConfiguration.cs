using _10xCards.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _10xCards.Database.Configurations;

public sealed class GenerationErrorLogConfiguration : IEntityTypeConfiguration<GenerationErrorLog> {
    public void Configure(EntityTypeBuilder<GenerationErrorLog> builder) {
        builder.ToTable("generation_error_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Model)
            .HasColumnName("model")
            .IsRequired();

        builder.Property(e => e.SourceTextHash)
            .HasColumnName("source_text_hash")
            .IsRequired();

        builder.Property(e => e.SourceTextLength)
            .HasColumnName("source_text_length")
            .IsRequired();

        builder.Property(e => e.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("now()");

        // CHECK constraint for source_text_length
        builder.ToTable(tb => tb.HasCheckConstraint(
            "CK_generation_error_logs_source_text_length",
            "source_text_length BETWEEN 1000 AND 10000"
        ));

        // Foreign key relationship
        builder.HasOne(e => e.User)
            .WithMany(u => u.GenerationErrorLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_generation_error_logs_user_id");
    }
}

