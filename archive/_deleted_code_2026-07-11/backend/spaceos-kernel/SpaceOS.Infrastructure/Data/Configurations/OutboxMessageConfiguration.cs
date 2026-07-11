// SpaceOS.Infrastructure/Data/Configurations/OutboxMessageConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Outbox;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="OutboxMessage"/>.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.ProcessedAt);

        builder.Property(m => m.TenantId)
            .IsRequired();

        // Phase 4 extension fields (nullable for backward compatibility)
        builder.Property(m => m.BatchId);
        builder.Property(m => m.BatchSequenceNumber);
        builder.Property(m => m.AggregateId);

        builder.Property(m => m.AggregateType)
            .HasMaxLength(200);

        builder.Property(m => m.EventType)
            .HasMaxLength(200);

        builder.Property(m => m.Status)
            .HasConversion<int>()
            .HasDefaultValue(OutboxStatus.Pending)
            .IsRequired();

        builder.Property(m => m.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(m => m.LastError)
            .HasMaxLength(2000);

        // Index for Status-based polling (replaces ProcessedAt IS NULL pattern)
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.ProcessedAt);
        builder.HasIndex(m => m.TenantId);

        // UNIQUE constraint: no duplicate sequence numbers within a batch
        builder.HasIndex(m => new { m.BatchId, m.BatchSequenceNumber })
            .HasDatabaseName("IX_OutboxMessages_BatchId_SeqNum")
            .IsUnique()
            .HasFilter("\"BatchId\" IS NOT NULL");
    }
}
