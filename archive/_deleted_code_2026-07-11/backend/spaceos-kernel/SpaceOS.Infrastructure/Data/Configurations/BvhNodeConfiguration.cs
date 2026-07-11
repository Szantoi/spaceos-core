// SpaceOS.Infrastructure/Data/Configurations/BvhNodeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="BvhNode"/> entity.
/// </summary>
public class BvhNodeConfiguration : IEntityTypeConfiguration<BvhNode>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BvhNode> builder)
    {
        builder.ToTable("BvhNodes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TenantId).IsRequired();

        builder.Property(n => n.PhysicalSpaceId).IsRequired();

        builder.OwnsOne(n => n.BoundingBox, bb =>
        {
            bb.Property(b => b.MinX).HasColumnName("MinX").IsRequired();
            bb.Property(b => b.MinY).HasColumnName("MinY").IsRequired();
            bb.Property(b => b.MinZ).HasColumnName("MinZ").IsRequired();
            bb.Property(b => b.MaxX).HasColumnName("MaxX").IsRequired();
            bb.Property(b => b.MaxY).HasColumnName("MaxY").IsRequired();
            bb.Property(b => b.MaxZ).HasColumnName("MaxZ").IsRequired();
        });

        builder.Property(n => n.IsLeaf).IsRequired();

        builder.HasOne<BvhNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasIndex(n => n.PhysicalSpaceId);
    }
}
