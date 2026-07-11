using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryReorderOutboxConfiguration : IEntityTypeConfiguration<InventoryReorderOutbox>
{
    public void Configure(EntityTypeBuilder<InventoryReorderOutbox> builder)
    {
        builder.ToTable("InventoryReorderOutboxes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.NextAttemptAt).IsRequired();
        builder.Property(x => x.LeaseUntil);
        builder.Property(x => x.AttemptCount).HasDefaultValue(0);
        builder.Property(x => x.LastError).HasMaxLength(2000);

        // Polling index: Pending rows ordered by NextAttemptAt
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });

        // Tenant index for DiD assertions
        builder.HasIndex(x => x.TenantId);
    }
}
