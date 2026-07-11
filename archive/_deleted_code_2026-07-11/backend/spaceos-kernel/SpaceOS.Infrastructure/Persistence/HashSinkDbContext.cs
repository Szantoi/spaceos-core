// SpaceOS.Infrastructure/Persistence/HashSinkDbContext.cs

using Microsoft.EntityFrameworkCore;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// Isolated EF Core database context for the <c>spaceos_audit_sink</c> database.
/// Connects using the restricted <c>spaceos_sink_writer</c> PostgreSQL role which
/// only has <c>INSERT</c> privileges on <c>hash_chain_records</c>.
/// </summary>
/// <remarks>
/// <para>
/// This context is registered via <c>AddDbContextFactory&lt;HashSinkDbContext&gt;()</c>
/// (not <c>AddDbContext</c>) so that the factory can create short-lived, immediately
/// disposed contexts for each sink write. This avoids the Scoped-lifetime disposal
/// risk of fire-and-forget sink writes that outlive the originating HTTP request scope.
/// </para>
/// <para>
/// Migrations for this context must be generated and applied with:
/// <code>
///   dotnet ef migrations add &lt;Name&gt;
///     --context HashSinkDbContext
///     --output-dir Migrations/HashSink
///     --project SpaceOS.Infrastructure
///     --startup-project SpaceOS.Kernel.Api
///   dotnet ef database update
///     --context HashSinkDbContext
///     --connection "Host=...;Database=spaceos_audit_sink;Username=spaceos_sink_writer;Password=..."
/// </code>
/// </para>
/// <para>
/// <b>Escrow upgrade gate (OFF):</b> The PostgreSQL hash sink (two DBs, one instance)
/// is acceptable for Phase 1.5. Escrow GA requires upgrading the sink to S3 Object Lock
/// or Azure Immutable Blob before the escrow feature flag can be enabled.
/// </para>
/// </remarks>
internal sealed class HashSinkDbContext(DbContextOptions<HashSinkDbContext> options) : DbContext(options)
{
    /// <summary>Gets the <see cref="HashChainRecord"/> entities.</summary>
    public DbSet<HashChainRecord> HashChainRecords => Set<HashChainRecord>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<HashChainRecord>(entity =>
        {
            entity.ToTable("hash_chain_records");

            entity.HasKey(r => r.Id);

            // bigserial in PostgreSQL — ValueGeneratedOnAdd() without an explicit column type so
            // SQLite (used in integration tests via EnsureCreated) maps it to INTEGER AUTOINCREMENT.
            // The PostgreSQL bigint / bigserial mapping is enforced via the migration annotation.
            entity.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            entity.Property(r => r.TenantId)
                .IsRequired();

            entity.Property(r => r.EventId)
                .IsRequired();

            entity.HasIndex(r => r.EventId)
                .IsUnique()
                .HasDatabaseName("IX_hash_chain_records_EventId");

            entity.Property(r => r.StateHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(r => r.OccurredAt)
                .IsRequired();

            entity.Property(r => r.InsertedAt)
                .IsRequired()
                // SQLite doesn't support now() as a default — use ValueGeneratedOnAdd as a fallback.
                // In PostgreSQL the migration applies DEFAULT now() explicitly.
                .HasDefaultValueSql("(datetime('now'))");

            entity.HasIndex(r => r.TenantId)
                .HasDatabaseName("IX_hash_chain_records_TenantId");

            entity.HasIndex(r => r.OccurredAt)
                .HasDatabaseName("IX_hash_chain_records_OccurredAt");
        });

        base.OnModelCreating(modelBuilder);
    }
}
