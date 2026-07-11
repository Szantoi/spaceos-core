// Identity.Infrastructure/Persistence/Configurations/SpaceOSUserConfiguration.cs

using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class SpaceOSUserConfiguration : IEntityTypeConfiguration<SpaceOSUser>
{
    public void Configure(EntityTypeBuilder<SpaceOSUser> builder)
    {
        builder.ToTable("spaceos_users", "identity");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => SpaceOSUserId.From(value));

        builder.Property(u => u.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired()
            .HasConversion(
                email => email.Value,
                value => Email.From(value));

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(u => u.KcSyncStatus)
            .HasColumnName("kc_sync_status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(u => u.KeycloakUserId)
            .HasColumnName("keycloak_user_id")
            .HasMaxLength(100)
            .IsRequired(false)
            .HasConversion(
                id => id == null ? null : id.Value,
                value => value == null ? null : KeycloakUserId.From(value));

        builder.Property(u => u.OperatorPin)
            .HasColumnName("operator_pin")
            .HasMaxLength(4)
            .IsRequired(false)
            .HasConversion(
                pin => pin == null ? null : pin.Value,
                value => OperatorPin.FromString(value));

        builder.ComplexProperty(u => u.DisplayName, dn =>
        {
            dn.Property(d => d.FirstName)
              .HasColumnName("first_name")
              .HasMaxLength(100)
              .IsRequired();
            dn.Property(d => d.LastName)
              .HasColumnName("last_name")
              .HasMaxLength(100)
              .IsRequired();
        });

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // UNIQUE(email, tenant_id)
        builder.HasIndex(u => new { u.Email, u.TenantId })
            .IsUnique()
            .HasDatabaseName("uq_spaceos_users_email_tenant");

        // Partial UNIQUE keycloak_user_id WHERE NOT NULL (via raw SQL in migration)
        // Composite index: tenant_id + status
        builder.HasIndex(u => new { u.TenantId, u.Status })
            .HasDatabaseName("idx_spaceos_users_tenant_status");

        // Partial index for kc_sync_status (Pending/Failed) — defined in migration
        // Self-ref FK for created_by: defined as shadow property in migration
        builder.Ignore(u => u.DomainEvents);
    }
}
