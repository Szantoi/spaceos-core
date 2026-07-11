using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Outbox;

/// <summary>
/// Creates a dedicated, unscoped <see cref="SalesDbContext"/> for the background worker.
/// The worker must not share the request-scoped DbContext because it runs outside
/// the HTTP pipeline.
/// </summary>
public interface ISalesWorkerDbContextFactory
{
    /// <summary>Creates a fresh DbContext for one processing iteration.</summary>
    Task<SalesDbContext> CreateAsync(CancellationToken ct);
}

/// <summary>
/// Default implementation that creates a context from the worker connection string.
/// Does NOT register <see cref="TenantSessionInterceptor"/> — the worker sets the GUC
/// manually per message via <c>set_config</c>.
/// </summary>
internal sealed class SalesWorkerDbContextFactory(string workerConnectionString)
    : ISalesWorkerDbContextFactory
{
    /// <inheritdoc/>
    public Task<SalesDbContext> CreateAsync(CancellationToken ct)
    {
        var opts = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(workerConnectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_sales"))
            .Options;

        return Task.FromResult(new SalesDbContext(opts));
    }
}
