using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Infrastructure.Outbox;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the spaceos_sales schema.
/// Money Currency post-load fix applied via ChangeTracker.Tracked (BE-S-04).
/// </summary>
public sealed class SalesDbContext : DbContext
{
    /// <summary>Initialises a new instance and wires the post-load Money fix.</summary>
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
        // BE-S-04: after EF materialises a Quote from a query, restore the Currency
        // into the Money structs that only store Amount (TotalNet/TotalVat/TotalGross).
        // Must be in the constructor — ChangeTracker is not accessible inside OnModelCreating.
        ChangeTracker.Tracked += (_, e) =>
        {
            if (e.Entry.Entity is Quote q && e.FromQuery)
                q.FixMoneyCurrency();
        };
    }

    /// <summary>Customer aggregate root set.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Quote aggregate root set.</summary>
    public DbSet<Quote> Quotes => Set<Quote>();

    /// <summary>Quote line items (owned by Quote).</summary>
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();

    /// <summary>Transactional outbox messages.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Immutable audit log entries.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("spaceos_sales");
        mb.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
