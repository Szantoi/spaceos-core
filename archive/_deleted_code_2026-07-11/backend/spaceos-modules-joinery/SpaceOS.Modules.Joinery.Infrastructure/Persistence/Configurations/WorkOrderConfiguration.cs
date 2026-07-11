using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ConfigurationId).IsRequired();
        builder.Property(e => e.Quantity).IsRequired();
        builder.Property(e => e.DeliveryDate).HasColumnType("date").IsRequired();
        builder.Property(e => e.CustomerRef).HasMaxLength(100);
        builder.Property(e => e.Notes).HasColumnType("text");
        builder.Property(e => e.BomItems).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.TotalMaterialCost).HasColumnType("numeric(12,2)");
        builder.Property(e => e.EstimatedLabor).HasColumnType("numeric(12,2)");
        builder.Property(e => e.TotalCost).HasColumnType("numeric(12,2)");
        builder.Property(e => e.ScheduledStart).HasColumnType("date");
        builder.Property(e => e.EstimatedCompletion).HasColumnType("date");
        builder.Property(e => e.PdfUrl).HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.ConfigurationId);
        builder.HasIndex(e => e.DeliveryDate);
    }
}
