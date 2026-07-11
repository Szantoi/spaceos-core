// SpaceOS.Infrastructure/Data/Configurations/SpatialTaskLinkConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="SpatialTaskLink"/> entity.
/// </summary>
public class SpatialTaskLinkConfiguration : IEntityTypeConfiguration<SpatialTaskLink>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SpatialTaskLink> builder)
    {
        builder.ToTable("SpatialTaskLinks");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TenantId).IsRequired();

        builder.Property(l => l.FlowTaskId).IsRequired();

        builder.Property(l => l.SpatialElementId).IsRequired();

        builder.Property(l => l.WorkPhase)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(new[] { "FlowTaskId", "SpatialElementId" }).IsUnique();

        builder.HasIndex(l => l.FlowTaskId);
        builder.HasIndex(l => l.SpatialElementId);
    }
}
