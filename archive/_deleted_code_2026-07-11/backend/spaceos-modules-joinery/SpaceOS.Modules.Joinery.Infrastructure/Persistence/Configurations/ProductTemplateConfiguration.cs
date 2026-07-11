using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class ProductTemplateConfiguration : IEntityTypeConfiguration<ProductTemplate>
{
    public void Configure(EntityTypeBuilder<ProductTemplate> builder)
    {
        builder.ToTable("ProductTemplates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(50);
        builder.Property(e => e.DimensionRules).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.AllowedMaterials).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.AllowedFittings).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.PricingRules).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.LeadTimeDays).HasDefaultValue(7);
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz");
    }
}
