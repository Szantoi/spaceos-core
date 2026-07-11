using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("sales_audit_log");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).UseIdentityAlwaysColumn();

        b.Property(a => a.TenantId).IsRequired();
        b.Property(a => a.ActorSub).HasMaxLength(200).IsRequired();
        b.Property(a => a.AggregateType).HasMaxLength(50).IsRequired();
        b.Property(a => a.AggregateId).IsRequired();
        b.Property(a => a.Operation).HasMaxLength(50).IsRequired();
        b.Property(a => a.PayloadHash).HasMaxLength(64).IsFixedLength().IsRequired();

        b.HasIndex(a => new { a.TenantId, a.AggregateType, a.AggregateId })
            .HasDatabaseName("IX_SalesAudit_Tenant_Aggregate");
    }
}
