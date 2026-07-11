// Identity.Infrastructure/Persistence/Configurations/KcSyncOutboxConfiguration.cs

using Identity.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class KcSyncOutboxConfiguration : IEntityTypeConfiguration<KcSyncOutboxEntry>
{
    public void Configure(EntityTypeBuilder<KcSyncOutboxEntry> builder)
    {
        builder.ToTable("kc_sync_outbox", "identity");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Operation)
            .HasColumnName("operation")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(e => e.Payload).HasColumnName("payload").IsRequired(false);
        builder.Property(e => e.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        // Shadow properties for worker tracking
        builder.Property<DateTimeOffset?>("ProcessedAt").HasColumnName("processed_at").IsRequired(false);
        builder.Property<DateTimeOffset?>("LastAttemptAt").HasColumnName("last_attempt_at").IsRequired(false);

        // Partial index WHERE processed_at IS NULL — defined as raw SQL annotation in migration
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_kc_sync_outbox_created_at");
    }
}
