using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions.Domain.Entities;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence.Configurations;

public sealed class GeometryAttachmentConfiguration : IEntityTypeConfiguration<GeometryAttachment>
{
    public void Configure(EntityTypeBuilder<GeometryAttachment> builder)
    {
        builder.ToTable("GeometryAttachments");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.TenantId).IsRequired();
        builder.Property(g => g.SlotInstanceId).IsRequired();
        builder.Property(g => g.Level).HasColumnType("varchar(20)").HasConversion<string>().IsRequired();
        builder.Property(g => g.SpatialElementId);
        builder.Property(g => g.SkeletonJson).HasColumnType("jsonb");
        builder.Property(g => g.FileReference).HasMaxLength(500);
        builder.Property(g => g.FileFormat).HasColumnType("varchar(10)");
        builder.Property(g => g.FileHash).HasMaxLength(64);
        builder.Property(g => g.CreatedAt).IsRequired();

        builder.HasIndex(g => g.SlotInstanceId).HasDatabaseName("IX_GeometryAttachments_SlotInstanceId");
        builder.HasIndex(g => g.SpatialElementId)
               .HasDatabaseName("IX_GeometryAttachments_SpatialElementId")
               .HasFilter("\"SpatialElementId\" IS NOT NULL");
    }
}
