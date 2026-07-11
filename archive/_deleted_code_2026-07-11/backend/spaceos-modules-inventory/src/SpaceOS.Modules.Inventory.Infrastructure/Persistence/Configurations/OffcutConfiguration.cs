using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class OffcutConfiguration : IEntityTypeConfiguration<Offcut>
{
    public void Configure(EntityTypeBuilder<Offcut> builder)
    {
        builder.ToTable("Offcuts", "spaceos_inventory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MaterialCatalogId).IsRequired();
        builder.Property(x => x.MaterialCode).HasMaxLength(100);
        builder.Property(x => x.WidthMm).HasPrecision(10, 2);
        builder.Property(x => x.HeightMm).HasPrecision(10, 2);
        builder.Property(x => x.ThicknessMm).HasPrecision(10, 2);
        builder.Property(x => x.VolumeM3).HasPrecision(18, 9);
        builder.Property(x => x.WeightKg).HasPrecision(10, 3);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
        // spec: (status, createdAt desc) and (volumeM3 desc)
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.VolumeM3);
    }
}
