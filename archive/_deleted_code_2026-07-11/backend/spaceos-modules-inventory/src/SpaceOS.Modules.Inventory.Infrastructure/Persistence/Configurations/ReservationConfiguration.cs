using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", "spaceos_inventory");
        builder.HasKey(x => x.Id);

        // xmin-based optimistic concurrency: map the PostgreSQL system column to RowVersion.
        // IsRowVersion() marks it as a concurrency token and sets ValueGeneratedOnAddOrUpdate.
        builder.Property(x => x.RowVersion)
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .IsRowVersion()
               .ValueGeneratedOnAddOrUpdate();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.ConsumerModule).IsRequired().HasMaxLength(50);

        // jsonb — nullable
        builder.Property(x => x.ConsumerContextJson)
               .HasColumnType("jsonb")
               .IsRequired(false);

        builder.Property(x => x.CreatedByUserId).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.Property(x => x.Status)
               .HasConversion<int>()
               .IsRequired();

        // Composite index supports the partial unique index and typical lookup patterns
        builder.HasIndex(x => new { x.TenantId, x.CorrelationId });
    }
}
