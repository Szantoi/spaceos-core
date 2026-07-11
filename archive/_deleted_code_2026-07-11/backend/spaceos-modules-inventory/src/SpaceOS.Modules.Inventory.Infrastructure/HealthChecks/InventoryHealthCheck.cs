using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using SpaceOS.Modules.Inventory.Infrastructure.Services;

namespace SpaceOS.Modules.Inventory.Infrastructure.HealthChecks;

public sealed class InventoryHealthCheck : IHealthCheck
{
    private readonly InventoryDbContext _ctx;
    private readonly IWorkerHeartbeatStore _heartbeat;

    public InventoryHealthCheck(InventoryDbContext ctx, IWorkerHeartbeatStore heartbeat)
    {
        _ctx = ctx;
        _heartbeat = heartbeat;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();

        try { await _ctx.Database.ExecuteSqlRawAsync("SELECT 1", ct).ConfigureAwait(false); }
        catch (Exception ex) { return HealthCheckResult.Unhealthy("DB unreachable", ex); }

        var lastRun = await _heartbeat.GetLastTickAsync("inventory-cleanup-worker", ct).ConfigureAwait(false);
        var workerStatus = lastRun switch
        {
            null => "never",
            _ when DateTimeOffset.UtcNow - lastRun.Value < TimeSpan.FromMinutes(30) => "healthy",
            _ => "stale"
        };

        data["api"] = "Healthy";
        data["cleanupWorker"] = new { lastRun, status = workerStatus };

        return workerStatus == "stale"
            ? HealthCheckResult.Degraded("Cleanup worker heartbeat stale (> 30 min)", data: data)
            : HealthCheckResult.Healthy("All components operational", data);
    }
}
