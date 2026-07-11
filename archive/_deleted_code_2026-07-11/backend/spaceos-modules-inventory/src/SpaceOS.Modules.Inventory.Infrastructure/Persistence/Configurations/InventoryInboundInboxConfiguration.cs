using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryInboundInboxConfiguration : IEntityTypeConfiguration<InventoryInboundInbox>
{
    public void Configure(EntityTypeBuilder<InventoryInboundInbox> builder)
    {
        builder.ToTable("InventoryInboundInboxes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.DeliveryLineId).IsRequired();
        builder.Property(x => x.MaterialCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(14, 4);
        builder.Property(x => x.UnitOfMeasure).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired();

        // Compound idempotency key
        builder.HasIndex(x => new { x.TenantId, x.DeliveryLineId }).IsUnique();
    }
}
