using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Anyaglista aggregate.
/// RLS: FORCE — TenantId must match current_setting('app.tenant_id').
/// </summary>
public sealed class AnyaglistaConfiguration : IEntityTypeConfiguration<Domain.Core.Anyaglista>
{
    public void Configure(EntityTypeBuilder<Domain.Core.Anyaglista> builder)
    {
        builder.ToTable("Anyaglistak");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.OrderId)
            .IsRequired();

        builder.Property(a => a.PdfContent)
            .IsRequired(false);

        builder.Property(a => a.StorageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasIndex(a => a.OrderId)
            .HasDatabaseName("IX_Anyaglistak_OrderId");
    }
}
