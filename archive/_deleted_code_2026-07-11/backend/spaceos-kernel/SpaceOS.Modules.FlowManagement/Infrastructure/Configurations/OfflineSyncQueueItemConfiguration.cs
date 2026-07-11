// SpaceOS.Modules.FlowManagement/Infrastructure/Configurations/OfflineSyncQueueItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.FlowManagement.Domain;

namespace SpaceOS.Modules.FlowManagement.Infrastructure.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="OfflineSyncQueueItem"/>.
/// </summary>
internal sealed class OfflineSyncQueueItemConfiguration : IEntityTypeConfiguration<OfflineSyncQueueItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OfflineSyncQueueItem> builder)
    {
        builder.ToTable("OfflineSyncQueue");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.TenantId)
            .IsRequired();

        builder.Property(q => q.Payload)
            .IsRequired();

        builder.Property(q => q.ClientSignalId)
            .IsRequired();

        builder.Property(q => q.ExpiresAt)
            .IsRequired();

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        // Idempotency guard — one queued item per (tenant, client-generated ID).
        builder.HasIndex(q => new { q.TenantId, q.ClientSignalId })
            .IsUnique()
            .HasDatabaseName("UIX_OfflineSyncQueue_TenantId_ClientSignalId");

        // Supports efficient purge of expired items.
        builder.HasIndex(q => q.ExpiresAt)
            .HasDatabaseName("IX_OfflineSyncQueue_ExpiresAt");
    }
}
