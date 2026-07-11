using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class ProductionSheetCacheConfiguration : IEntityTypeConfiguration<ProductionSheetCache>
{
    public void Configure(EntityTypeBuilder<ProductionSheetCache> builder)
    {
        builder.ToTable("ProductionSheetCache");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.SnapshotId).IsRequired();
        builder.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.FileHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.GeneratedAt).IsRequired();

        builder.HasOne<CuttingListSnapshot>()
            .WithOne()
            .HasForeignKey<ProductionSheetCache>(e => e.SnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on SnapshotId enforced by the FK-to-unique-PK relationship above
        builder.HasIndex(e => e.SnapshotId).IsUnique().HasDatabaseName("UX_ProductionSheetCache_SnapshotId");
    }
}
