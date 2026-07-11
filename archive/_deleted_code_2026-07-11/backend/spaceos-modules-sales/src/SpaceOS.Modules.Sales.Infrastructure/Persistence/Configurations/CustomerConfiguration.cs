using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(c => c.Id);

        b.Property(c => c.TenantId).IsRequired();
        b.Property(c => c.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(c => c.CompanyTaxNumber).HasMaxLength(50);
        b.Property(c => c.ContactName).HasMaxLength(200).IsRequired();
        b.Property(c => c.Notes).HasMaxLength(2000);
        b.Property(c => c.CreatedBy).HasMaxLength(200).IsRequired();

        // BE-S-10: enums stored as strings
        b.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(c => c.LinkStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Email value object — stored as nullable string
        b.Property(c => c.ContactEmail)
            .HasColumnName("ContactEmail")
            .HasMaxLength(320)
            .HasConversion(
                e => e == null ? null : e.Value,
                s => s == null ? null : Email.From(s).Value);

        // PhoneNumber value object — stored as nullable string
        b.Property(c => c.ContactPhone)
            .HasColumnName("ContactPhone")
            .HasMaxLength(50)
            .HasConversion(
                p => p == null ? null : p.Value,
                s => s == null ? null : PhoneNumber.From(s).Value);

        // Address value objects stored as JSONB
        b.Property(c => c.BillingAddress)
            .HasColumnName("BillingAddressJson")
            .HasColumnType("jsonb")
            .HasConversion(
                a => a == null ? null : JsonSerializer.Serialize(a, (JsonSerializerOptions?)null),
                s => s == null ? null : JsonSerializer.Deserialize<Address>(s, (JsonSerializerOptions?)null));

        b.Property(c => c.ShippingAddress)
            .HasColumnName("ShippingAddressJson")
            .HasColumnType("jsonb")
            .HasConversion(
                a => a == null ? null : JsonSerializer.Serialize(a, (JsonSerializerOptions?)null),
                s => s == null ? null : JsonSerializer.Deserialize<Address>(s, (JsonSerializerOptions?)null));

        // xmin rowversion — Npgsql handles this natively (BE-S-01)
        b.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        b.HasIndex(c => c.TenantId).HasDatabaseName("IX_Customers_TenantId");
    }
}
