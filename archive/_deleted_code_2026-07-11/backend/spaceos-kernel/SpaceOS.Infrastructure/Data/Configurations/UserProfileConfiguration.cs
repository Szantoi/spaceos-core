// SpaceOS.Infrastructure/Data/Configurations/UserProfileConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.UserProfiles;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="UserProfile"/>.
/// Maps the GDPR pseudonymization table used to bridge real user identities
/// (JWT <c>sub</c> claims) to stable pseudonym GUIDs stored in the audit log.
/// </summary>
internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.Id)
            .ValueGeneratedNever();

        builder.Property(up => up.ExternalUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(up => up.TenantId)
            .IsRequired();

        builder.Property(up => up.CreatedAt)
            .IsRequired();

        builder.Property(up => up.IsErased)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique constraint: one pseudonym per (user, tenant) pair.
        builder.HasIndex(up => new { up.ExternalUserId, up.TenantId })
            .IsUnique();

        // Index for efficient tenant-scoped erasure lookups.
        builder.HasIndex(up => up.TenantId);
    }
}
