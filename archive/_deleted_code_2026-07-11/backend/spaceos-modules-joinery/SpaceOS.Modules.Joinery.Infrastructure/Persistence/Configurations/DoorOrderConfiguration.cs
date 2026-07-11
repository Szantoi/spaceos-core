using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class DoorOrderConfiguration : IEntityTypeConfiguration<DoorOrder>
{
    public void Configure(EntityTypeBuilder<DoorOrder> builder)
    {
        builder.ToTable("DoorOrders");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FlowEpicId).IsRequired();
        builder.Property(e => e.ProjectId).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ProjectName).HasMaxLength(200).IsRequired(false);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.CalculationError).HasMaxLength(2000).IsRequired(false);
        builder.Property(e => e.Version).IsRequired().HasDefaultValue(1).IsConcurrencyToken();
        builder.OwnsOne(e => e.ProjectInfo, pi =>
        {
            pi.Property(x => x.ClientName).HasColumnName("ClientName").HasMaxLength(200);
            pi.Property(x => x.ClientAddress).HasColumnName("ClientAddress").HasMaxLength(500);
            pi.Property(x => x.ClientPhone).HasColumnName("ClientPhone").HasMaxLength(50);
            pi.Property(x => x.DeliveryDate).HasColumnName("DeliveryDate");
        });

        // Conversion fields
        builder.Property(o => o.CustomerId).IsRequired(false);
        builder.Property(o => o.LinkedTenantId).IsRequired(false);
        builder.Property(o => o.SourceQuoteId).IsRequired(false);
        builder.Property(o => o.SourceContentHash).HasMaxLength(256).IsRequired(false);
        builder.Property(o => o.ConfirmedFromSalesAt).IsRequired(false);
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired(false);
        builder.Property(o => o.TotalNet).HasColumnType("decimal(18,4)").IsRequired(false);
        builder.Property(o => o.TotalVat).HasColumnType("decimal(18,4)").IsRequired(false);
        builder.Property(o => o.TotalGross).HasColumnType("decimal(18,4)").IsRequired(false);

        // Partial unique index for idempotency (only rows where SourceQuoteId IS NOT NULL)
        builder.HasIndex(o => new { o.TenantId, o.SourceQuoteId })
            .IsUnique()
            .HasFilter("\"SourceQuoteId\" IS NOT NULL")
            .HasDatabaseName("UX_DoorOrders_TenantId_SourceQuoteId");

        // DoorOrderConvertedLines owned collection
        builder.OwnsMany(o => o.ConvertedLines, lines =>
        {
            lines.ToTable("DoorOrderConvertedLines");
            lines.HasKey(l => l.Id);
            lines.WithOwner().HasForeignKey("DoorOrderId");
            lines.Property(l => l.SourceTemplateId).IsRequired(false);
            lines.Property(l => l.Description).HasMaxLength(500).IsRequired();
            lines.Property(l => l.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            lines.Property(l => l.UnitPriceNet).HasColumnType("decimal(18,4)").IsRequired();
            lines.Property(l => l.VatRate).HasColumnType("decimal(6,4)").IsRequired();
            lines.Property(l => l.DiscountPercent).HasColumnType("decimal(6,4)").IsRequired(false);
            lines.Property(l => l.SortOrder).IsRequired().HasDefaultValue(0);
            lines.HasIndex("DoorOrderId").HasDatabaseName("IX_DoorOrderConvertedLines_OrderId");
        });

        builder.HasMany(e => e.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_DoorOrders_TenantId");
        builder.HasIndex(e => e.FlowEpicId).HasDatabaseName("IX_DoorOrders_FlowEpicId");
        builder.Ignore(e => e.DomainEvents);
    }
}
