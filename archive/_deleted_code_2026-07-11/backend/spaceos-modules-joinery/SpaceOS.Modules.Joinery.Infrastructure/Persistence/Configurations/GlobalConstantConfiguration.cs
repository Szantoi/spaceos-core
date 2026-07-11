using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class GlobalConstantConfiguration : IEntityTypeConfiguration<GlobalConstant>
{
    public void Configure(EntityTypeBuilder<GlobalConstant> builder)
    {
        builder.ToTable("GlobalConstants");
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Value).HasColumnType("numeric(18,6)").IsRequired();
    }
}
