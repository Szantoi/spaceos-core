// SpaceOS.Infrastructure/Persistence/AuditDbContext.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Infrastructure.Data.Configurations;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// Isolated EF Core database context for the <see cref="AuditEvent"/> aggregate.
/// Uses a dedicated connection string (<c>ConnectionStrings:AuditWriter</c>) so that the
/// <c>spaceos_audit_writer</c> PostgreSQL role — which only has INSERT + SELECT on
/// <c>AuditEvents</c> — is used for every audit write.  Row-level security on the
/// <c>AuditEvents</c> table is therefore enforced at the DB level for all audit traffic.
/// </summary>
/// <remarks>
/// <para>
/// This context intentionally owns only the <c>AuditEvents</c> table.  All other aggregates
/// are owned by <see cref="SpaceOS.Infrastructure.Data.AppDbContext"/>.
/// </para>
/// <para>
/// Migrations for this context must be generated and applied with:
/// <code>
///   dotnet ef migrations add &lt;Name&gt; --context AuditDbContext --output-dir Migrations/Audit
///   dotnet ef database update --context AuditDbContext
/// </code>
/// </para>
/// </remarks>
public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    /// <summary>Gets the <see cref="AuditEvent"/> entities.</summary>
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());

        // AuditEventConfiguration sets HasDefaultValue(0L) on Sequence so that SQLite
        // (EnsureCreated, integration tests) creates an INTEGER NOT NULL DEFAULT 0 column
        // and never sends an explicit value on insert.
        // On PostgreSQL the migration adds GENERATED ALWAYS AS IDENTITY, which forbids
        // explicit values in INSERT entirely. UseIdentityAlwaysColumn() replaces the SQLite
        // default annotation with the correct Npgsql sentinel so EF Core never includes
        // Sequence in INSERT column lists — the DB assigns it automatically.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<AuditEvent>()
                .Property(ae => ae.Sequence)
                .UseIdentityAlwaysColumn();
        }

        base.OnModelCreating(modelBuilder);
    }
}
