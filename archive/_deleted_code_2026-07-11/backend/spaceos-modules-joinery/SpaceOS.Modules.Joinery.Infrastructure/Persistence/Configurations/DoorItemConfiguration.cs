using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class DoorItemConfiguration : IEntityTypeConfiguration<DoorItem>
{
    public void Configure(EntityTypeBuilder<DoorItem> builder)
    {
        builder.ToTable("DoorItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OrderId).IsRequired();
        builder.Property(e => e.Sorszam).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Quantity).IsRequired();
        builder.Property(e => e.DoorType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.OpeningDirection).HasConversion<string>().HasMaxLength(15).IsRequired();

        builder.OwnsOne(e => e.Dimensions, d =>
        {
            d.Property(x => x.WallOpeningWidth).HasColumnName("WallOpeningWidth").HasColumnType("numeric(10,2)");
            d.Property(x => x.DoorWidth).HasColumnName("DoorWidth").HasColumnType("numeric(10,2)");
            d.Property(x => x.WallOpeningHeight).HasColumnName("WallOpeningHeight").HasColumnType("numeric(10,2)");
            d.Property(x => x.DoorHeight).HasColumnName("DoorHeight").HasColumnType("numeric(10,2)");
            d.Property(x => x.WallOpeningThickness).HasColumnName("WallOpeningThickness").HasColumnType("numeric(10,2)");
            d.Property(x => x.DoorThickness).HasColumnName("DoorThickness").HasColumnType("numeric(10,2)");
        });

        builder.OwnsOne(e => e.FixSide, f =>
        {
            f.Property(x => x.SurfaceType).HasColumnName("FixSurfaceType").HasConversion<string>().HasMaxLength(30);
            f.Property(x => x.Color).HasColumnName("FixColor").HasMaxLength(100);
            f.Property(x => x.ColorCode).HasColumnName("FixColorCode").HasMaxLength(50);
            f.Property(x => x.Pattern).HasColumnName("FixPattern").HasMaxLength(100);
            f.Property(x => x.PatternType).HasColumnName("FixPatternType").HasMaxLength(100);
            f.Property(x => x.PatternProfile).HasColumnName("FixPatternProfile").HasMaxLength(100);
            f.Property(x => x.CoatingColor).HasColumnName("FixCoatingColor").HasMaxLength(100);
            f.Property(x => x.HasBlende).HasColumnName("FixHasBlende");
            f.Property(x => x.HasWallPanel).HasColumnName("FixHasWallPanel");
        });

        builder.OwnsOne(e => e.MovingSide, m =>
        {
            m.Property(x => x.SurfaceType).HasColumnName("MovSurfaceType").HasConversion<string>().HasMaxLength(30);
            m.Property(x => x.Color).HasColumnName("MovColor").HasMaxLength(100);
            m.Property(x => x.ColorCode).HasColumnName("MovColorCode").HasMaxLength(50);
            m.Property(x => x.Pattern).HasColumnName("MovPattern").HasMaxLength(100);
            m.Property(x => x.PatternType).HasColumnName("MovPatternType").HasMaxLength(100);
            m.Property(x => x.PatternProfile).HasColumnName("MovPatternProfile").HasMaxLength(100);
            m.Property(x => x.CoatingColor).HasColumnName("MovCoatingColor").HasMaxLength(100);
            m.Property(x => x.HasBlende).HasColumnName("MovHasBlende");
            m.Property(x => x.HasWallPanel).HasColumnName("MovHasWallPanel");
        });

        builder.OwnsOne(e => e.Glazing, g =>
        {
            g.Property(x => x.GlazingType).HasColumnName("GlazingType").HasMaxLength(100);
            g.Property(x => x.GlazingColor).HasColumnName("GlazingColor").HasMaxLength(100);
            g.Property(x => x.GlazingStyle).HasColumnName("GlazingStyle").HasMaxLength(100);
            g.Property(x => x.GlazingPattern).HasColumnName("GlazingPattern").HasMaxLength(100);
        });

        builder.OwnsOne(e => e.Hardware, h =>
        {
            h.Property(x => x.LockType).HasColumnName("LockType").HasMaxLength(100);
            h.Property(x => x.LockSize).HasColumnName("LockSize").HasMaxLength(50);
            h.Property(x => x.StrikeType).HasColumnName("StrikeType").HasMaxLength(100);
            h.Property(x => x.HandleType).HasColumnName("HandleType").HasMaxLength(100);
            h.Property(x => x.HandleColor).HasColumnName("HandleColor").HasMaxLength(100);
            h.Property(x => x.HandleKit).HasColumnName("HandleKit").HasMaxLength(100);
            h.Property(x => x.KeyholeDrilling).HasColumnName("KeyholeDrilling");
            h.Property(x => x.AutoThreshold).HasColumnName("AutoThreshold");
            h.Property(x => x.PanelTensioner).HasColumnName("PanelTensioner");
            h.Property(x => x.HingeType).HasColumnName("HingeType").HasMaxLength(100);
            h.Property(x => x.HingeCount).HasColumnName("HingeCount");
            h.Property(x => x.HingeSpacing).HasColumnName("HingeSpacing").HasMaxLength(50);
            h.Property(x => x.HingeColor).HasColumnName("HingeColor").HasMaxLength(100);
            h.Property(x => x.EdgeStripType).HasColumnName("EdgeStripType").HasMaxLength(100);
            h.Property(x => x.EdgeStripColor).HasColumnName("EdgeStripColor").HasMaxLength(100);
            h.Property(x => x.SealType).HasColumnName("SealType").HasMaxLength(100);
            h.Property(x => x.SealColor).HasColumnName("SealColor").HasMaxLength(100);
        });

        builder.OwnsOne(e => e.Materials, m =>
        {
            m.Property(x => x.FrameMaterial).HasColumnName("FrameMaterial").HasMaxLength(100);
            m.Property(x => x.InsertMaterial).HasColumnName("InsertMaterial").HasMaxLength(100);
            m.Property(x => x.CladMaterial).HasColumnName("CladMaterial").HasMaxLength(100);
            m.Property(x => x.FrameCoreMaterial).HasColumnName("FrameCoreMaterial").HasMaxLength(100);
            m.Property(x => x.BlendeMaterial).HasColumnName("BlendeMaterial").HasMaxLength(100);
            m.Property(x => x.CoatingMaterial).HasColumnName("CoatingMaterial").HasMaxLength(100);
        });

        builder.OwnsOne(e => e.Processing, p =>
        {
            p.Property(x => x.CncProcessing).HasColumnName("CncProcessing").HasMaxLength(200);
            p.Property(x => x.PanelProcessing).HasColumnName("PanelProcessing").HasMaxLength(200);
            p.Property(x => x.FrameProcessing).HasColumnName("FrameProcessing").HasMaxLength(200);
            p.Property(x => x.Note).HasColumnName("ProcessingNote").HasMaxLength(500);
        });

        builder.HasIndex(e => e.OrderId).HasDatabaseName("IX_DoorItems_OrderId");
    }
}
