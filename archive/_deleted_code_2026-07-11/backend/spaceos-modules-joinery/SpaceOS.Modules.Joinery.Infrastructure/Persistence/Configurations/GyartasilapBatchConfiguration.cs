using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for GyartasilapBatch aggregate.
/// GyartasilapIds stored as JSONB for compact list serialization.
/// RLS: FORCE — TenantId must match current_setting('app.tenant_id').
/// </summary>
public sealed class GyartasilapBatchConfiguration : IEntityTypeConfiguration<GyartasilapBatch>
{
    public void Configure(EntityTypeBuilder<GyartasilapBatch> builder)
    {
        builder.ToTable("GyartasilapBatches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.TenantId)
            .IsRequired();

        builder.Property(b => b.OrderId)
            .IsRequired();

        builder.Property<List<Guid>>("_gyartasilapIds")
            .HasColumnName("GyartasilapIds")
            .HasColumnType("jsonb")
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!);

        builder.Ignore(b => b.GyartasilapIds);

        builder.Property(b => b.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.ZipStoragePath)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.CompletedAt)
            .IsRequired(false);

        builder.HasIndex(b => new { b.OrderId, b.Status })
            .HasDatabaseName("IX_GyartasilapBatches_OrderId_Status");
    }
}
