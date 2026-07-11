using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Gyartasilap aggregate.
/// RLS: inherit tenant isolation via JoineryOrderId FK.
/// Indexes: rapid lookup by order + plan.
/// </summary>
public sealed class GyartasilapConfiguration : IEntityTypeConfiguration<Gyartasilap>
{
    public void Configure(EntityTypeBuilder<Gyartasilap> builder)
    {
        builder.ToTable("Gyartasilaps");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.TenantId)
            .IsRequired();

        builder.Property(g => g.JoineryOrderId)
            .IsRequired();

        builder.Property(g => g.CuttingPlanId)
            .IsRequired(false);

        builder.Property(g => g.Version)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(g => g.PdfContent)
            .IsRequired(false);

        builder.Property(g => g.StorageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(g => g.LabelVariant)
            .HasMaxLength(5)
            .IsRequired()
            .HasConversion(
                v => v,
                v => v);

        builder.Property(g => g.Status)
            .HasConversion<int>();

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .IsRequired(false);

        // ── Indexes ────────────────────────────────────────────────────────────

        // Rapid order → all gyartasilaps lookup
        builder.HasIndex(g => new { g.JoineryOrderId, g.Status })
            .HasDatabaseName("IX_Gyartasilaps_OrderId_Status");

        // Cutting plan lookup + temporal sort
        builder.HasIndex(g => new { g.CuttingPlanId, g.CreatedAt })
            .HasDatabaseName("IX_Gyartasilaps_PlanId_CreatedAt");

        // Tenant isolation implicit via RLS on parent JoineryOrder
        // RLS policy: WHERE tenant_id = current_setting('app.tenant_id')::uuid
    }
}
