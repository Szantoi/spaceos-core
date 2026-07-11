// SpaceOS.Infrastructure/Data/Configurations/TenantHandshakeAllowlistConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="TenantHandshakeAllowlist"/>.
/// No global query filter — both guest and host tenants need read access.
/// </summary>
internal sealed class TenantHandshakeAllowlistConfiguration
    : IEntityTypeConfiguration<TenantHandshakeAllowlist>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TenantHandshakeAllowlist> builder)
    {
        builder.ToTable("TenantHandshakeAllowlist");

        builder.HasKey(x => new { x.GuestTenantId, x.HostTenantId });

        // Use a portable JSON converter so SQLite (unit/integration tests) can store
        // the list as a text column without PostgreSQL array syntax errors.
        // OnModelCreating in AppDbContext overrides HasColumnType to varchar(32)[]
        // when running against PostgreSQL.
        builder.Property<List<string>>("_allowedTradeTypes")
            .HasColumnName("AllowedTradeTypes")
            .HasConversion(TenantConfiguration.StringListJsonConverter)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
