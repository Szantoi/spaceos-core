using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence.Configurations;

public sealed class ProductTemplateConfiguration : IEntityTypeConfiguration<ProductTemplate>
{
    public void Configure(EntityTypeBuilder<ProductTemplate> builder)
    {
        builder.ToTable("ProductTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.TradeType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Version).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.IsArchived).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => t.TenantId).HasDatabaseName("IX_ProductTemplates_TenantId");
        builder.HasIndex(t => new { t.TenantId, t.TradeType })
               .HasDatabaseName("IX_ProductTemplates_TenantId_TradeType")
               .HasFilter("\"IsActive\" = true AND \"IsArchived\" = false");
        builder.HasIndex(t => new { t.TenantId, t.Name, t.Version }).IsUnique();

        // Navigation: owned collections live in their own DbSets
        builder.HasMany<Domain.Entities.ComponentSlot>()
               .WithOne()
               .HasForeignKey("TemplateId")
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<Domain.Entities.SlotConnection>()
               .WithOne()
               .HasForeignKey("TemplateId")
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<Domain.Entities.TemplateParameter>()
               .WithOne()
               .HasForeignKey("TemplateId")
               .OnDelete(DeleteBehavior.Cascade);

        // No public setters — configure backing fields
        builder.Navigation(t => t.Slots).HasField("_slots").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Connections).HasField("_connections").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Parameters).HasField("_parameters").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
