using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class OffcutBatchConfiguration : IEntityTypeConfiguration<OffcutBatch>
{
    public void Configure(EntityTypeBuilder<OffcutBatch> builder)
    {
        builder.ToTable("OffcutBatches", "spaceos_inventory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SourceType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourceId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceId }).IsUnique();
    }
}
