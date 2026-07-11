using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Api.Endpoints;

/// <summary>
/// Internal endpoints — only callable with X-SpaceOS-Internal header.
/// Used by the Orchestrator for test data reset (BE-TEST-05).
/// SEC-TS-01: TEST_TENANT_ALLOWLIST env var restricts which tenants can be reset.
/// </summary>
public static class InternalEndpoints
{
    private const string InternalHeader = "X-SpaceOS-Internal";
    private const string LoggerCategory = "SpaceOS.Inventory.Internal";

    public static void MapInternalInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // DELETE /internal/panel-stocks/by-tenant/{tenantId}?confirm=true
        app.MapDelete("/internal/panel-stocks/by-tenant/{tenantId}", async (
            string tenantId,
            bool? confirm,
            HttpContext ctx,
            InventoryDbContext db,
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

            // Pin a single connection so set_config and all deletes share the
            // same physical connection — prevents pool GUC contamination (22P02).
            // IsRelational() is false for InMemory provider (tests): skip there.
            if (db.Database.IsRelational())
                await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);

            int stockMovements, offcuts, panelStocks;
            try
            {
                // Set tenant GUC manually — no Bearer token in internal calls.
                if (db.Database.IsRelational())
                    await db.Database.ExecuteSqlRawAsync(
                        "SELECT set_config('app.current_tenant_id', {0}, false)",
                        tenantGuid.ToString()).ConfigureAwait(false);

                // Delete in FK-safe order: StockMovements → Offcuts → PanelStocks
                var movementsToDelete = await db.StockMovements
                    .Where(x => x.TenantId == tenantGuid)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                db.StockMovements.RemoveRange(movementsToDelete);

                var offcutsToDelete = await db.Offcuts
                    .Where(x => x.TenantId == tenantGuid)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                db.Offcuts.RemoveRange(offcutsToDelete);

                var panelStocksToDelete = await db.PanelStocks
                    .Where(x => x.TenantId == tenantGuid)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                db.PanelStocks.RemoveRange(panelStocksToDelete);

                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                stockMovements = movementsToDelete.Count;
                offcuts = offcutsToDelete.Count;
                panelStocks = panelStocksToDelete.Count;
            }
            finally
            {
                if (db.Database.IsRelational())
                    await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            }

            logger.LogInformation(
                "InternalDeleteByTenant: tenant {TenantId} reset — deleted {StockMovements} stockMovements, {Offcuts} offcuts, {PanelStocks} panelStocks",
                tenantId, stockMovements, offcuts, panelStocks);

            return Results.Ok(new
            {
                tenantId,
                deletedCounts = new
                {
                    panelStocks,
                    offcuts,
                    stockMovements
                }
            });
        })
        .AllowAnonymous()  // Auth is header-based (X-SpaceOS-Internal), not JWT
        .WithTags("Internal");
    }
}
