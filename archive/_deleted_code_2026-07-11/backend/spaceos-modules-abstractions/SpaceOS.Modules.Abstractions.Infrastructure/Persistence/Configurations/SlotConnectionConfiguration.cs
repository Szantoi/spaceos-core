using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions.Domain.Entities;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence.Configurations;

public sealed class SlotConnectionConfiguration : IEntityTypeConfiguration<SlotConnection>
{
    public void Configure(EntityTypeBuilder<SlotConnection> builder)
    {
        builder.ToTable("SlotConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.TemplateId).IsRequired();
        builder.Property(c => c.ParentSlotId).IsRequired();
        builder.Property(c => c.ChildSlotId).IsRequired();
        builder.Property(c => c.Axis).HasColumnType("varchar(10)").HasConversion<string>().IsRequired();
        builder.Property(c => c.Operator).HasColumnType("varchar(20)").HasConversion<string>().IsRequired();
        builder.Property(c => c.Operand).HasColumnType("decimal(8,3)").IsRequired();
        builder.Property(c => c.MultiplierCount);
        builder.Property(c => c.SecondaryParentSlotId);
        builder.Property(c => c.JointType).HasColumnType("varchar(30)").HasConversion<string>().IsRequired();
        builder.Property(c => c.MachiningOp).HasColumnType("varchar(20)").HasConversion<string>().IsRequired();
        builder.Property(c => c.ProcessPhase).HasColumnType("varchar(20)").HasConversion<string>().IsRequired();
        builder.Property(c => c.GrooveDepth).HasColumnType("decimal(6,2)");
        builder.Property(c => c.GrooveWidth).HasColumnType("decimal(6,2)");
        builder.Property(c => c.DrillDiameter).HasColumnType("decimal(6,2)");
        builder.Property(c => c.DrillDepth).HasColumnType("decimal(6,2)");
        builder.Property(c => c.Angle).HasColumnType("decimal(6,2)");
        builder.Property(c => c.Radius).HasColumnType("decimal(6,2)");
        builder.Property(c => c.JointNote).HasMaxLength(200);

        builder.HasIndex(c => c.TemplateId).HasDatabaseName("IX_SlotConnections_TemplateId");
        builder.HasIndex(c => c.ParentSlotId).HasDatabaseName("IX_SlotConnections_ParentSlotId");
        builder.HasIndex(c => c.ChildSlotId).HasDatabaseName("IX_SlotConnections_ChildSlotId");
        builder.HasIndex(c => new { c.TemplateId, c.ParentSlotId, c.ChildSlotId, c.Axis }).IsUnique();
    }
}
