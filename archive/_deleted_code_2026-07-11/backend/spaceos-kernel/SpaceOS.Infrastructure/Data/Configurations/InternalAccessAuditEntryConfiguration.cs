using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Infrastructure.Internal;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="InternalAccessAuditEntry"/>.
/// </summary>
internal sealed class InternalAccessAuditEntryConfiguration
    : IEntityTypeConfiguration<InternalAccessAuditEntry>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InternalAccessAuditEntry> builder)
    {
        builder.ToTable("InternalAccessAuditLog");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();
        builder.Property(e => e.RequesterTenantId).IsRequired();
        builder.Property(e => e.TargetTenantId).IsRequired();
        builder.Property(e => e.Result).HasMaxLength(20).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.RequesterTenantId, e.OccurredAt })
            .HasDatabaseName("IX_InternalAudit_Requester_OccurredAt");
        builder.HasIndex(e => new { e.TargetTenantId, e.OccurredAt })
            .HasDatabaseName("IX_InternalAudit_Target_OccurredAt");
    }
}
