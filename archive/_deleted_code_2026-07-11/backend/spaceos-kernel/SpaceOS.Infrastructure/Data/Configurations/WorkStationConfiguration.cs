using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="WorkStation"/> aggregate root.
/// </summary>
public class WorkStationConfiguration : IEntityTypeConfiguration<WorkStation>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<WorkStation> builder)
    {
        builder.ToTable("WorkStations");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasConversion(
                id => id.Value,
                value => WorkStationId.From(value));

        builder.Property(w => w.Name)
            .HasConversion(
                name => name.Value,
                value => WorkStationName.From(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Type)
            .HasConversion(
                type => type.Value,
                value => WorkStationType.From(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.FacilityId)
            .HasConversion(
                id => id.Value,
                value => FacilityId.From(value))
            .IsRequired();

        builder.HasIndex(w => w.FacilityId);

        builder.Property(w => w.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.HasIndex(w => w.TenantId);
    }
}
