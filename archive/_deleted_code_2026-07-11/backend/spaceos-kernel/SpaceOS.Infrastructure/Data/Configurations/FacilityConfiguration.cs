using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Facility"/> aggregate root.
/// </summary>
public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("Facilities");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(
                id => id.Value,
                value => FacilityId.From(value));

        builder.Property(f => f.Name)
            .HasConversion(
                name => name.Value,
                value => FacilityName.From(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.HasIndex(f => f.TenantId);
    }
}
