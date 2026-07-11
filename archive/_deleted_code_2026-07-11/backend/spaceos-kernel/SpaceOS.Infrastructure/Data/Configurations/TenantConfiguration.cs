using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Tenant"/> aggregate root.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <summary>
    /// JSON-based value converter for <c>List&lt;string&gt;</c>.
    /// Used as a portable fallback for SQLite (unit/integration tests).
    /// PostgreSQL overrides the column type to <c>varchar(32)[]</c> in
    /// <see cref="AppDbContext.OnModelCreating"/> after configurations are applied.
    /// </summary>
    internal static readonly ValueConverter<List<string>, string> StringListJsonConverter =
        new(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>());

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value));

        builder.Property(t => t.Name)
            .HasConversion(
                name => name.Value,
                value => TenantName.From(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.BrandSkinId)
            .HasMaxLength(64)
            .IsRequired(false);

        // ADR-018: varchar(32) + explicit conversion (BE-02: avoid double-conversion with JsonStringEnumConverter).
        // HasDefaultValue ensures existing rows materialise as Manufacturer when no value is stored.
        builder.Property(t => t.TenantType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TenantType>(v))
            .HasColumnType("varchar(32)")
            .HasColumnName("TenantType")
            .IsRequired()
            .HasDefaultValue(TenantType.Manufacturer);

        // SEC: EmailHash stores SHA-256(email) — never the raw address.
        // Indexed for O(1) lookup by the AttributionWorker (PartnerTier).
        builder.Property(t => t.EmailHash)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.HasIndex(t => t.EmailHash)
            .IsUnique()
            .HasFilter("\"EmailHash\" IS NOT NULL");

        builder.Ignore(t => t.EnabledModules);

        // Use a portable JSON converter so SQLite (unit/integration tests) can store
        // the list as a text column without PostgreSQL array syntax errors.
        // OnModelCreating in AppDbContext overrides HasColumnType to varchar(32)[]
        // when running against PostgreSQL.
        builder.Property<List<string>>("_enabledModules")
            .HasColumnName("EnabledModules")
            .HasConversion(StringListJsonConverter)
            .HasDefaultValueSql("'[]'")
            .IsRequired();
    }
}
