// SpaceOS.Infrastructure/Data/Configurations/NodeManifestConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="NodeManifest"/> aggregate root.
/// </summary>
internal sealed class NodeManifestConfiguration : IEntityTypeConfiguration<NodeManifest>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<NodeManifest> builder)
    {
        builder.ToTable("NodeManifests");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        // Each tenant may only register one node manifest.
        builder.HasIndex(n => n.TenantId)
            .IsUnique();

        builder.Property(n => n.ServerUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(n => n.PublicApiVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.Version)
            .IsConcurrencyToken();

        builder.Property(n => n.MaxGuestLod)
            .IsRequired();

        builder.Property(n => n.LastHeartbeatAt);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.UpdatedAt).IsRequired();
    }
}
