// SpaceOS.Infrastructure/Data/Configurations/SpatialElementConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="SpatialElement"/> entity.
/// </summary>
public class SpatialElementConfiguration : IEntityTypeConfiguration<SpatialElement>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SpatialElement> builder)
    {
        builder.ToTable("SpatialElements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.BvhLeafId).IsRequired();

        builder.Property(e => e.FlowEpicId).IsRequired();

        builder.Property(e => e.TradeType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ElementType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IsArchived)
            .HasDefaultValue(false);

        builder.HasIndex(e => e.BvhLeafId).IsUnique();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.FlowEpicId);
    }
}
