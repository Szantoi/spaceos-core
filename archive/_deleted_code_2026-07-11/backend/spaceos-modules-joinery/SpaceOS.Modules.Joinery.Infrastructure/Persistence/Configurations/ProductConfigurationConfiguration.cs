using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class ProductConfigurationConfiguration : IEntityTypeConfiguration<ProductConfiguration>
{
    public void Configure(EntityTypeBuilder<ProductConfiguration> builder)
    {
        builder.ToTable("ProductConfigurations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ProductType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Params).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.BomSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.EstimatedPrice).HasColumnType("numeric(10,2)");
        builder.Property(e => e.PreviewUrl).HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ProductType);
        builder.HasIndex(e => e.CreatedAt).IsDescending();
    }
}
