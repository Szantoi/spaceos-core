using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Configurations;

internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> b)
    {
        b.ToTable("Quotes");
        b.HasKey(q => q.Id);

        b.Property(q => q.TenantId).IsRequired();
        b.Property(q => q.CustomerId).IsRequired();

        // QuoteNumber value object — stored as string column
        b.Property(q => q.Number)
            .HasColumnName("QuoteNumber")
            .HasMaxLength(15)
            .HasConversion(n => n.Value, s => new QuoteNumber(s))
            .IsRequired();

        b.Property(q => q.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(q => q.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        b.Property(q => q.Notes).HasMaxLength(2000);
        b.Property(q => q.ContentHash).HasColumnName("ContentHash").HasMaxLength(64).IsFixedLength();
        b.Property(q => q.RejectionReason).HasMaxLength(500);
        b.Property(q => q.ConversionFailureReason).HasMaxLength(1000);
        b.Property(q => q.CreatedBy).HasMaxLength(200).IsRequired();

        // BE-S-04: Money totals — only Amount stored; Currency reconstructed from Quote.Currency at load time.
        b.Property(q => q.TotalNet)
            .HasColumnName("TotalNetAmount")
            .HasColumnType("decimal(14,2)")
            .HasConversion(m => m.Amount, a => new Money(a, string.Empty));

        b.Property(q => q.TotalVat)
            .HasColumnName("TotalVatAmount")
            .HasColumnType("decimal(14,2)")
            .HasConversion(m => m.Amount, a => new Money(a, string.Empty));

        b.Property(q => q.TotalGross)
            .HasColumnName("TotalGrossAmount")
            .HasColumnType("decimal(14,2)")
            .HasConversion(m => m.Amount, a => new Money(a, string.Empty));

        // Lines as owned collection in a separate table
        b.OwnsMany(q => q.Lines, lb =>
        {
            lb.ToTable("QuoteLines");
            lb.WithOwner().HasForeignKey("QuoteId");
            lb.HasKey(l => l.Id);
            lb.Property(l => l.TenantId).IsRequired();
            lb.Property(l => l.LineType).HasConversion<string>().HasMaxLength(20).IsRequired();
            lb.Property(l => l.Description).HasMaxLength(500).IsRequired();
            lb.Property(l => l.Quantity).HasColumnType("decimal(12,3)").IsRequired();
            lb.Property(l => l.VatRate).HasColumnType("decimal(5,4)").IsRequired();
            lb.Property(l => l.DiscountPercent).HasColumnType("decimal(5,4)");
            lb.Property(l => l.SortOrder).IsRequired();

            // UnitPrice: Amount stored; Currency restored at load time from Quote.Currency
            lb.Property(l => l.UnitPrice)
                .HasColumnName("UnitPriceAmount")
                .HasColumnType("decimal(14,2)")
                .HasConversion(m => m.Amount, a => new Money(a, string.Empty));

            // Shadow property for the currency column in QuoteLines (used in DB but not in domain model)
            lb.Property<string>("UnitPriceCurrency").HasMaxLength(3).IsFixedLength().HasDefaultValue("HUF");

            lb.Property(l => l.LineNet)
                .HasColumnName("LineNetAmount")
                .HasColumnType("decimal(14,2)")
                .HasConversion(m => m.Amount, a => new Money(a, string.Empty));

            lb.Property(l => l.LineVat)
                .HasColumnName("LineVatAmount")
                .HasColumnType("decimal(14,2)")
                .HasConversion(m => m.Amount, a => new Money(a, string.Empty));

            lb.Property(l => l.LineGross)
                .HasColumnName("LineGrossAmount")
                .HasColumnType("decimal(14,2)")
                .HasConversion(m => m.Amount, a => new Money(a, string.Empty));
        });

        // xmin rowversion (BE-S-01)
        b.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        b.HasIndex(q => new { q.TenantId, q.Status }).HasDatabaseName("IX_Quotes_Tenant_Status");
    }
}
