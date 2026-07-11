using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class MaterialCatalogConfiguration : IEntityTypeConfiguration<MaterialCatalog>
{
    public void Configure(EntityTypeBuilder<MaterialCatalog> builder)
    {
        builder.ToTable("MaterialCatalogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200);
        builder.Property(x => x.SupplierRef).HasMaxLength(100);
        builder.Property(x => x.StandardWidth).HasPrecision(10, 2);
        builder.Property(x => x.StandardHeight).HasPrecision(10, 2);
        builder.Property(x => x.ThicknessMm).HasPrecision(5, 1);
        builder.Property(x => x.UnitCost).HasPrecision(10, 4);
        builder.Property(x => x.ReorderPoint).HasDefaultValue(5);
        builder.Property(x => x.SuggestedOrderQuantity).HasDefaultValue(10);
        builder.Property(x => x.UnitOfMeasure).HasMaxLength(20).HasDefaultValue("pcs");
        builder.Property(x => x.PreferredSupplierId);
        builder.HasIndex(x => x.MaterialType).IsUnique();

        // Seed data
        builder.HasData(
            new { Id = new Guid("10000000-0000-0000-0000-000000000001"), MaterialType = "MDF 18mm", Description = "MDF lap 18mm", StandardWidth = 2800m, StandardHeight = 2070m, ThicknessMm = 18m, UnitCost = 8500m, SupplierRef = "MDF-18", ReorderPoint = 5, SuggestedOrderQuantity = 10, UnitOfMeasure = "pcs", PreferredSupplierId = (Guid?)null },
            new { Id = new Guid("10000000-0000-0000-0000-000000000002"), MaterialType = "MDF 16mm", Description = "MDF lap 16mm", StandardWidth = 2800m, StandardHeight = 2070m, ThicknessMm = 16m, UnitCost = 7800m, SupplierRef = "MDF-16", ReorderPoint = 5, SuggestedOrderQuantity = 10, UnitOfMeasure = "pcs", PreferredSupplierId = (Guid?)null },
            new { Id = new Guid("10000000-0000-0000-0000-000000000003"), MaterialType = "HDF 3mm", Description = "HDF lap 3mm", StandardWidth = 2800m, StandardHeight = 2070m, ThicknessMm = 3m, UnitCost = 3200m, SupplierRef = "HDF-3", ReorderPoint = 5, SuggestedOrderQuantity = 10, UnitOfMeasure = "pcs", PreferredSupplierId = (Guid?)null },
            new { Id = new Guid("10000000-0000-0000-0000-000000000004"), MaterialType = "Forgácslap 18mm", Description = "Forgácslap 18mm", StandardWidth = 2800m, StandardHeight = 2070m, ThicknessMm = 18m, UnitCost = 5500m, SupplierRef = "PART-18", ReorderPoint = 5, SuggestedOrderQuantity = 10, UnitOfMeasure = "pcs", PreferredSupplierId = (Guid?)null },
            new { Id = new Guid("10000000-0000-0000-0000-000000000005"), MaterialType = "ABS él 0.8mm", Description = "ABS élzáró szalag 0.8mm", StandardWidth = 50m, StandardHeight = 25000m, ThicknessMm = 0.8m, UnitCost = 1200m, SupplierRef = "ABS-08", ReorderPoint = 5, SuggestedOrderQuantity = 10, UnitOfMeasure = "pcs", PreferredSupplierId = (Guid?)null }
        );
    }
}
