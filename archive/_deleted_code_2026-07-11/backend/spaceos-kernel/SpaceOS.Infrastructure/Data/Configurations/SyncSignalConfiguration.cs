// SpaceOS.Infrastructure/Data/Configurations/SyncSignalConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="SyncSignal"/> aggregate root.
/// </summary>
internal sealed class SyncSignalConfiguration : IEntityTypeConfiguration<SyncSignal>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SyncSignal> builder)
    {
        builder.ToTable("SyncSignals");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EpicId)
            .HasConversion(
                id => id.Value,
                value => FlowEpicId.From(value))
            .IsRequired();

        builder.Property(s => s.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(s => s.NewState)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.StateHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.PreviousHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.ClientSignalId)
            .IsRequired();

        builder.Property(s => s.IsSyncedToKernel)
            .IsRequired();

        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.OccurredAt).IsRequired();

        // Idempotency guard — one signal per (tenant, client-generated ID).
        builder.HasIndex(s => new { s.TenantId, s.ClientSignalId })
            .IsUnique();

        // Efficient ordered retrieval for last-hash queries and chain replay.
        // IX_sync_signals_tenant_occurred
        builder.HasIndex(s => new { s.TenantId, s.OccurredAt })
            .HasDatabaseName("IX_sync_signals_tenant_occurred");

        // Efficient lookup of all signals belonging to a given epic.
        // IX_sync_signals_epic
        builder.HasIndex(s => s.EpicId)
            .HasDatabaseName("IX_sync_signals_epic");
    }
}
