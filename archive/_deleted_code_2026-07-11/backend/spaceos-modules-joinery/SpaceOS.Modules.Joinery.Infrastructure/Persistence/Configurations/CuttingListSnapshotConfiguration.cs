using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class CuttingListSnapshotConfiguration : IEntityTypeConfiguration<CuttingListSnapshot>
{
    public void Configure(EntityTypeBuilder<CuttingListSnapshot> builder)
    {
        builder.ToTable("CuttingListSnapshots");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.DoorOrderId).IsRequired();
        builder.Property(e => e.DoorItemId).IsRequired();
        builder.Property(e => e.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TemplateVersion).IsRequired();
        builder.Property(e => e.InputWidth).HasColumnType("numeric(8,2)").IsRequired();
        builder.Property(e => e.InputHeight).HasColumnType("numeric(8,2)").IsRequired();
        builder.Property(e => e.ParameterOverridesJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();
        builder.Property(e => e.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.CalculatedAt).IsRequired();
        builder.Property(e => e.IsLatest).HasDefaultValue(true).IsRequired();

        builder.HasOne<Domain.Aggregates.DoorOrder>()
            .WithMany()
            .HasForeignKey(e => e.DoorOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.DoorItem>()
            .WithMany()
            .HasForeignKey(e => e.DoorItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lines as owned collection mapped to separate table
        builder.OwnsMany(e => e.Lines, line =>
        {
            line.ToTable("CuttingListLines");
            line.WithOwner().HasForeignKey("SnapshotId");
            line.Property<Guid>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property<Guid>("TenantId");
            line.Property(l => l.ComponentName).HasMaxLength(100).IsRequired();
            line.Property(l => l.ComponentType).HasMaxLength(50).IsRequired();
            line.Property(l => l.Width).HasColumnType("numeric(8,2)").IsRequired();
            line.Property(l => l.Height).HasColumnType("numeric(8,2)").IsRequired();
            line.Property(l => l.CuttingWidth).HasColumnType("numeric(8,2)").IsRequired();
            line.Property(l => l.CuttingHeight).HasColumnType("numeric(8,2)").IsRequired();
            line.Property(l => l.Material).HasMaxLength(100).IsRequired();
            line.Property(l => l.Thickness).HasColumnType("numeric(6,2)").IsRequired();
            line.Property(l => l.Quantity).IsRequired();
            line.Property(l => l.SortOrder).HasDefaultValue(0).IsRequired();
            line.HasIndex("SnapshotId").HasDatabaseName("IX_CuttingListLines_SnapshotId");
        });

        // CncInstructions as owned collection mapped to separate table
        builder.OwnsMany(e => e.CncInstructions, cnc =>
        {
            cnc.ToTable("CncInstructions");
            cnc.WithOwner().HasForeignKey("SnapshotId");
            cnc.Property<Guid>("Id").ValueGeneratedOnAdd();
            cnc.HasKey("Id");
            cnc.Property<Guid>("TenantId");
            cnc.Property(c => c.ComponentName).HasMaxLength(100).IsRequired();
            cnc.Property(c => c.Operation).HasMaxLength(30).IsRequired();
            cnc.Property(c => c.Position).HasMaxLength(200);
            cnc.Property(c => c.Diameter).HasColumnType("numeric(6,2)");
            cnc.Property(c => c.Depth).HasColumnType("numeric(6,2)");
            cnc.Property(c => c.Angle).HasColumnType("numeric(6,2)");
            cnc.Property(c => c.Note).HasMaxLength(500);
            cnc.HasIndex("SnapshotId").HasDatabaseName("IX_CncInstructions_SnapshotId");
        });

        // ProcessSteps as owned collection mapped to separate table
        builder.OwnsMany(e => e.ProcessSteps, step =>
        {
            step.ToTable("ProcessSteps");
            step.WithOwner().HasForeignKey("SnapshotId");
            step.Property<Guid>("Id").ValueGeneratedOnAdd();
            step.HasKey("Id");
            step.Property<Guid>("TenantId");
            step.Property(s => s.Phase).HasMaxLength(30).IsRequired();
            step.Property(s => s.StepOrder).IsRequired();
            step.Property(s => s.Description).HasMaxLength(500);
            step.Property(s => s.EstimatedSeconds).IsRequired();
            step.HasIndex("SnapshotId").HasDatabaseName("IX_ProcessSteps_SnapshotId");
        });

        builder.HasIndex(e => e.DoorOrderId).HasDatabaseName("IX_CuttingListSnapshots_DoorOrderId");
        builder.HasIndex(e => e.DoorItemId).HasDatabaseName("IX_CuttingListSnapshots_DoorItemId");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_CuttingListSnapshots_TenantId");
        // UX_CuttingListSnapshots_DoorItemId_Latest is a partial unique index — created in raw SQL migration (DB-03)
    }
}
