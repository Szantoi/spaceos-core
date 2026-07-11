using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="SpaceLayer"/> aggregate root.
/// </summary>
public class SpaceLayerConfiguration : IEntityTypeConfiguration<SpaceLayer>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SpaceLayer> builder)
    {
        builder.ToTable("SpaceLayers");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.Id)
            .HasConversion(
                id => id.Value,
                value => SpaceLayerId.From(value));

        builder.Property(sl => sl.FacilityId)
            .HasConversion(
                id => id.Value,
                value => FacilityId.From(value))
            .IsRequired();

        builder.Property(sl => sl.TradeType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(sl => sl.LastStateHash)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(sl => sl.IntentDataJson)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(sl => sl.ExternalSourceUrl)
            .HasMaxLength(2048)
            .IsRequired(false);

        // Stores the Key Vault reference name — resolve actual secret via ISecretProvider at runtime.
        builder.Property(sl => sl.ExternalAuthTokenRef)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(sl => sl.IsExternalNode)
            .IsRequired();

        builder.HasIndex(s => s.FacilityId);

        builder.Property(sl => sl.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.HasIndex(sl => sl.TenantId);
    }
}
