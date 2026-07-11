// Identity.Infrastructure/Persistence/IdentityDbContext.cs

using Identity.Application.Common;
using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public DbSet<SpaceOSUser> SpaceOSUsers => Set<SpaceOSUser>();
    public DbSet<KcSyncOutboxEntry> KcSyncOutbox => Set<KcSyncOutboxEntry>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    /// <summary>
    /// Executes SET LOCAL app.current_tenant_id before any mutating operation (DB-05 / RLS).
    /// Must be called inside an active transaction.
    /// </summary>
    public async Task SetTenantContextAsync(Guid tenantId, CancellationToken ct = default)
    {
        await Database.ExecuteSqlRawAsync(
            "SET LOCAL app.current_tenant_id = {0}", tenantId)
            .ConfigureAwait(false);
    }
}
