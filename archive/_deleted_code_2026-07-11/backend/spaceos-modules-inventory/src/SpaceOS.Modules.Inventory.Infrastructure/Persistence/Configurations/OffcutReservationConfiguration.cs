using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class OffcutReservationConfiguration : IEntityTypeConfiguration<OffcutReservation>
{
    public void Configure(EntityTypeBuilder<OffcutReservation> builder)
    {
        builder.ToTable("OffcutReservations", "spaceos_inventory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OffcutId).IsRequired();
        builder.Property(x => x.JobId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasIndex(x => x.OffcutId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.ExpiresAt);
    }
}
