// SpaceOS.Infrastructure/Data/Configurations/AggregateSnapshotConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Snapshots;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="AggregateSnapshot"/> append-only entity.
/// No UPDATE or DELETE paths are configured — rows are write-once.
/// </summary>
internal sealed class AggregateSnapshotConfiguration : IEntityTypeConfiguration<AggregateSnapshot>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AggregateSnapshot> builder)
    {
        builder.ToTable("AggregateSnapshots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.AggregateId)
            .IsRequired();

        builder.HasIndex(s => s.AggregateId);

        builder.Property(s => s.AggregateType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Version)
            .IsRequired();

        builder.Property(s => s.SnapshotAt)
            .IsRequired();

        builder.Property(s => s.TriggerEventId)
            .IsRequired();

        // No max length — state JSON can be arbitrarily large
        builder.Property(s => s.StateJson)
            .IsRequired();

        builder.Property(s => s.SnapshotHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.HasIndex(s => s.TenantId);
    }
}
