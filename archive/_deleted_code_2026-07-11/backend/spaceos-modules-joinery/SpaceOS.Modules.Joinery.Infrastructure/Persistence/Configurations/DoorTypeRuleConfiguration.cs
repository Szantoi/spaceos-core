using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class DoorTypeRuleConfiguration : IEntityTypeConfiguration<DoorTypeRule>
{
    public void Configure(EntityTypeBuilder<DoorTypeRule> builder)
    {
        builder.ToTable("DoorTypeRules");
        builder.HasKey(e => e.DoorType);
        builder.Property(e => e.DoorType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.BkmWidthFixed).HasColumnType("numeric(10,2)");
        builder.Property(e => e.BkmHeightFixed).HasColumnType("numeric(10,2)");
        builder.Property(e => e.BkmWidthMoving).HasColumnType("numeric(10,2)");
        builder.Property(e => e.BkmHeightMoving).HasColumnType("numeric(10,2)");
    }
}
