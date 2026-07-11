using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions.Domain.Entities;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence.Configurations;

public sealed class ComponentSlotConfiguration : IEntityTypeConfiguration<ComponentSlot>
{
    public void Configure(EntityTypeBuilder<ComponentSlot> builder)
    {
        builder.ToTable("ComponentSlots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.TemplateId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.ComponentType).HasColumnType("varchar(50)").IsRequired();
        builder.Property(s => s.SemanticRole).HasColumnType("varchar(20)");
        builder.Property(s => s.DefaultMaterial).HasMaxLength(100);
        builder.Property(s => s.DefaultThickness).HasColumnType("decimal(6,2)");
        builder.Property(s => s.Quantity).IsRequired();
        builder.Property(s => s.IsVirtual).IsRequired();
        builder.Property(s => s.SortOrder).IsRequired();

        builder.HasIndex(s => s.TemplateId).HasDatabaseName("IX_ComponentSlots_TemplateId");
    }
}
