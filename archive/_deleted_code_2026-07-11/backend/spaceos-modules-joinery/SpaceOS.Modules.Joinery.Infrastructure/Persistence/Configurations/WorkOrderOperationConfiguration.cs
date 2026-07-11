using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class WorkOrderOperationConfiguration : IEntityTypeConfiguration<WorkOrderOperation>
{
    public void Configure(EntityTypeBuilder<WorkOrderOperation> builder)
    {
        builder.ToTable("WorkOrderOperations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.WorkOrderId).IsRequired();
        builder.Property(e => e.Sequence).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.EstimatedDuration).IsRequired();
        builder.Property(e => e.OperationType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastModified).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();

        // Foreign key relationship
        builder.HasOne(e => e.WorkOrder)
            .WithMany()
            .HasForeignKey(e => e.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.WorkOrderId);
        builder.HasIndex(e => new { e.WorkOrderId, e.Sequence }).HasDatabaseName("IX_WorkOrderOperations_WorkOrderId_Sequence");
    }
}
