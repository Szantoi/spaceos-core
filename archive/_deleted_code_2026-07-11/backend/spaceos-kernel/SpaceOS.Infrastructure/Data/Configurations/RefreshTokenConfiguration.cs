// SpaceOS.Infrastructure/Data/Configurations/RefreshTokenConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Auth;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RefreshToken"/> entity.
/// Maps to the <c>RefreshTokens</c> table in <see cref="AppDbContext"/>.
/// </summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();

        b.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.ExpiresAt).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.RevokedAt); // nullable

        // Unique: each hash must appear at most once — prevents token reuse.
        b.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("UQ_RefreshTokens_TokenHash");

        // Supports efficient expired-token cleanup jobs.
        b.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        // Supports efficient per-user token lookup (e.g. revoke-all-devices).
        b.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
