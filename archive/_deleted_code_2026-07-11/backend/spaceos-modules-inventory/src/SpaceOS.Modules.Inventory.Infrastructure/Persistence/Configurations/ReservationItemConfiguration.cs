using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.ToTable("ReservationItems", "spaceos_inventory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.StockItemId).IsRequired();
        builder.Property(x => x.MaterialCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 4);
        builder.Property(x => x.QuantityConsumed).HasPrecision(18, 4).HasDefaultValue(0m);

        // FK back to Reservation with cascade delete — items have no meaning without parent
        builder.HasOne<Reservation>()
               .WithMany()
               .HasForeignKey(x => x.ReservationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.StockItemId);
    }
}
