using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Sales.Infrastructure.Outbox;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("sales_outbox");
        b.HasKey(m => m.Id);

        b.Property(m => m.TenantId).IsRequired();
        b.Property(m => m.AggregateId).IsRequired();
        b.Property(m => m.Operation).HasMaxLength(50).IsRequired();
        b.Property(m => m.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(m => m.IdempotencyKey).HasMaxLength(64).IsRequired();
        b.Property(m => m.Status).HasMaxLength(20).IsRequired();
        b.Property(m => m.LastError).HasMaxLength(2000);

        b.HasIndex(m => m.NextAttemptAt)
            .HasDatabaseName("IX_SalesOutbox_Pending")
            .HasFilter("\"Status\" IN ('Pending','InFlight')");
    }
}
