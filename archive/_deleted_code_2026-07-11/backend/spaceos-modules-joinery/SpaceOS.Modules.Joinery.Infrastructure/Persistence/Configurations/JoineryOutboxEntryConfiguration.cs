using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class JoineryOutboxEntryConfiguration : IEntityTypeConfiguration<JoineryOutboxEntry>
{
    public void Configure(EntityTypeBuilder<JoineryOutboxEntry> builder)
    {
        builder.ToTable("JoineryOutboxEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ProcessedAt);
        builder.Property(e => e.FailedAt);
        builder.Property(e => e.Error).HasMaxLength(2000);
        builder.Property(e => e.RetryCount).HasDefaultValue(0).IsRequired();

        // IX_JoineryOutboxEntries_Pending is a partial index — created in raw SQL migration
    }
}
