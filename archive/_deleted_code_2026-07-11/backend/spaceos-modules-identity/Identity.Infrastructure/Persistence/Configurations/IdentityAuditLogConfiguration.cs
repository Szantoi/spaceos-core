// Identity.Infrastructure/Persistence/Configurations/IdentityAuditLogConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Payload { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

internal sealed class IdentityAuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log", "identity");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired(false);
        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(50).IsRequired(false);
        builder.Property(e => e.TargetId).HasColumnName("target_id").IsRequired(false);
        builder.Property(e => e.Payload).HasColumnName("payload").IsRequired(false);
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();

        // Composite index (tenant_id, occurred_at DESC)
        builder.HasIndex(e => new { e.TenantId, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_log_tenant_occurred");
    }
}
