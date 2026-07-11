using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MaterialCatalogId).IsRequired();
        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Quantity).HasPrecision(10, 4);
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
        // Append-only: no update, no delete — enforced at application level
    }
}
