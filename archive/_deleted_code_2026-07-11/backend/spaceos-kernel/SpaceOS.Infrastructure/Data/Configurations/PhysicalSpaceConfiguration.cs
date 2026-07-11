// SpaceOS.Infrastructure/Data/Configurations/PhysicalSpaceConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Aggregates;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="PhysicalSpace"/> aggregate root.
/// </summary>
public class PhysicalSpaceConfiguration : IEntityTypeConfiguration<PhysicalSpace>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PhysicalSpace> builder)
    {
        builder.ToTable("PhysicalSpaces");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();

        builder.Property(s => s.FacilityId)
            .HasConversion(
                id => id.Value,
                value => FacilityId.From(value))
            .IsRequired();

        builder.ComplexProperty(s => s.Dimensions, d =>
        {
            d.Property(v => v.WidthMm).HasColumnName("WidthMm").IsRequired();
            d.Property(v => v.HeightMm).HasColumnName("HeightMm").IsRequired();
            d.Property(v => v.DepthMm).HasColumnName("DepthMm").IsRequired();
        });

        builder.ComplexProperty(s => s.Origin, o =>
        {
            o.Property(v => v.X).HasColumnName("OriginX").IsRequired();
            o.Property(v => v.Y).HasColumnName("OriginY").IsRequired();
            o.Property(v => v.Z).HasColumnName("OriginZ").IsRequired();
        });

        builder.Property(s => s.SpaceType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.CellSizeMm).IsRequired();

        builder.Property(s => s.RegistrationHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.FacilityId);

    }
}
