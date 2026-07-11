// SpaceOS.Infrastructure/Data/Configurations/AuditEventConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="AuditEvent"/> aggregate root.
/// Audit events are append-only — no Update or Delete operations are configured.
/// </summary>
internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.Id)
            .ValueGeneratedNever();

        builder.Property(ae => ae.TenantId)
            .IsRequired();

        builder.Property(ae => ae.EventType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ae => ae.AggregateId)
            .IsRequired();

        builder.Property(ae => ae.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(ae => ae.StateHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(ae => ae.PreviousHash)
            .HasMaxLength(64)
            .IsRequired()
            .HasDefaultValue("GENESIS");

        builder.Property(ae => ae.OccurredAt)
            .IsRequired();

        builder.Property(ae => ae.ActorId)
            .HasMaxLength(200);

        builder.Property(ae => ae.SourceIp)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(ae => ae.SourceBrand)
            .HasMaxLength(50)
            .IsRequired(false);

        // HashAlgorithm stored as string for readability in the database.
        // Max 20 covers "SHA3_256" and any near-future names. Default is "SHA256".
        builder.Property(ae => ae.HashAlgorithm)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(HashAlgorithmType.SHA256);

        // Monotone sequence assigned by the DB on insert — used as OccurredAt tiebreaker.
        // HasDefaultValue(0): SQLite EnsureCreatedAsync creates the column as INTEGER NOT NULL DEFAULT 0
        // so inserts succeed without application-provided values.
        // In PostgreSQL the migration adds GENERATED ALWAYS AS IDENTITY which supersedes the default.
        builder.Property(ae => ae.Sequence)
            .ValueGeneratedOnAdd()
            .HasDefaultValue(0L);

        // Composite index for efficient tenant-scoped time-range queries.
        builder.HasIndex(ae => new { ae.TenantId, ae.OccurredAt });
    }
}
