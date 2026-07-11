using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class PartDimensionRuleConfiguration : IEntityTypeConfiguration<PartDimensionRule>
{
    public void Configure(EntityTypeBuilder<PartDimensionRule> builder)
    {
        builder.ToTable("PartDimensionRules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DoorType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ComponentName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ComponentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Material).HasMaxLength(100);
        builder.Property(e => e.Thickness).HasColumnType("numeric(10,2)");
        builder.Property(e => e.WidthBase).HasColumnType("numeric(10,4)");
        builder.Property(e => e.WidthMultiplierFactor).HasColumnType("numeric(10,4)");
        builder.Property(e => e.LengthBase).HasColumnType("numeric(10,4)");
        builder.Property(e => e.LengthMultiplierFactor).HasColumnType("numeric(10,4)");
        builder.HasIndex(e => e.DoorType).HasDatabaseName("IX_PartDimensionRules_DoorType");
    }
}
