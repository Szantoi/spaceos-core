using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class PanelStockConfiguration : IEntityTypeConfiguration<PanelStock>
{
    public void Configure(EntityTypeBuilder<PanelStock> builder)
    {
        builder.ToTable("PanelStocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MaterialCatalogId).IsRequired();
        builder.Property(x => x.WidthMm).HasPrecision(10, 2);
        builder.Property(x => x.HeightMm).HasPrecision(10, 2);
        builder.Property(x => x.StockType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.LocationCode).HasMaxLength(50);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.MaterialCatalogId });
    }
}
