// SpaceOS.Infrastructure/Data/Configurations/ModuleSubscriptionConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="ModuleSubscription"/>.
/// </summary>
internal sealed class ModuleSubscriptionConfiguration : IEntityTypeConfiguration<ModuleSubscription>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ModuleSubscription> builder)
    {
        builder.ToTable("ModuleSubscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.SubscriberModule)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.EventType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.InboxEndpoint)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Prevents duplicate subscriptions for the same subscriber + event pair.
        builder.HasIndex(s => new { s.SubscriberModule, s.EventType })
            .HasDatabaseName("IX_ModuleSubscriptions_Sub_Event")
            .IsUnique();
    }
}
