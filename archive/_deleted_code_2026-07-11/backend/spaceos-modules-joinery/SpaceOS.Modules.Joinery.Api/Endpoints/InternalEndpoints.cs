using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

/// <summary>
/// Internal endpoints — only callable with X-SpaceOS-Internal header.
/// Used by the Orchestrator for test data reset (BE-TEST-03).
/// SEC-TS-01: TEST_TENANT_ALLOWLIST env var restricts which tenants can be reset.
/// </summary>
public static class InternalEndpoints
{
    private const string InternalHeader = "X-SpaceOS-Internal";
    private const string LoggerCategory = "SpaceOS.Joinery.Internal";

    // GUC key must match TenantSessionInterceptor.PgConfigKey
    private const string TenantGucKey = "app.tenant_id";

    public static void MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        // DELETE /internal/orders/by-tenant/{tenantId}?confirm=true
        app.MapDelete("/internal/orders/by-tenant/{tenantId}", async (
            string tenantId,
            bool? confirm,
            HttpContext ctx,
            IDoorOrderRepository repo,
            JoineryDbContext db,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(LoggerCategory);

            // SEC-TS-01: X-SpaceOS-Internal header required
            if (!ctx.Request.Headers.ContainsKey(InternalHeader))
            {
                logger.LogWarning("InternalDeleteByTenant: missing {Header} from {RemoteIp}",
                    InternalHeader, ctx.Connection.RemoteIpAddress);
                return Results.Forbid();
            }

            // confirm=true required to prevent accidental deletes
            if (confirm != true)
                return Results.BadRequest(new { error = "Bad request", message = "Missing confirm=true parameter" });

            // Validate tenantId format
            if (!Guid.TryParse(tenantId, out var tenantGuid))
                return Results.BadRequest(new { error = "Bad request", message = "Invalid tenantId format" });

            // SEC-TS-01: allowlist check — defense in depth against compromised Orchestrator
            var allowlistRaw = config["TEST_TENANT_ALLOWLIST"]
                ?? Environment.GetEnvironmentVariable("TEST_TENANT_ALLOWLIST")
                ?? string.Empty;

            var allowlist = allowlistRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!allowlist.Contains(tenantId))
            {
                logger.LogWarning("InternalDeleteByTenant: tenant {TenantId} not in TEST_TENANT_ALLOWLIST — rejected",
                    tenantId);
                return Results.Json(
                    new { error = "Forbidden", message = "Tenant not in test allowlist" },
                    statusCode: 403);
            }

            // GUC + CONNECTION AFFINITY FIX:
            // ExecuteSqlRawAsync and DeleteAllByTenantAsync can acquire different physical connections
            // from the pool. Opening the connection explicitly before set_config forces EF Core to
            // reuse the same physical connection for all subsequent operations on this DbContext,
            // ensuring the GUC value is visible to every query in the repository call.
            // Only executed on relational (PostgreSQL) providers; InMemory provider skips this.
            if (db.Database.IsRelational())
                await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);

            TenantDeletedCounts counts;
            try
            {
                if (db.Database.IsRelational())
                {
                    await db.Database.ExecuteSqlRawAsync(
                        $"SELECT set_config('{TenantGucKey}', {{0}}, false)",
                        tenantGuid.ToString())
                        .ConfigureAwait(false);
                }

                counts = await repo.DeleteAllByTenantAsync(tenantGuid, ct).ConfigureAwait(false);
            }
            finally
            {
                if (db.Database.IsRelational())
                    await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            }

            logger.LogInformation(
                "InternalDeleteByTenant: tenant {TenantId} reset — deleted {Orders} orders, {Snapshots} snapshots",
                tenantId, counts.DoorOrders, counts.CuttingListSnapshots);

            return Results.Ok(new
            {
                tenantId,
                deletedCounts = new
                {
                    doorOrders = counts.DoorOrders,
                    cuttingListSnapshots = counts.CuttingListSnapshots
                }
            });
        })
        .AllowAnonymous()  // Auth is header-based (X-SpaceOS-Internal), not JWT
        .WithTags("Internal");
    }
}
