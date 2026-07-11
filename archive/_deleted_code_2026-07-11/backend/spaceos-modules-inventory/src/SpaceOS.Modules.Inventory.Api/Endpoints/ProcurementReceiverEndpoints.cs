using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Api.Endpoints;

/// <summary>
/// Procurement integration receiver — POST /internal/inbound.
/// Called by the Procurement worker when a delivery is confirmed.
/// Auth: Bearer shared-secret (constant-time compare) + X-SpaceOS-TenantId header.
/// Idempotency: compound key (TenantId, DeliveryLineId).
/// One DB-tx: inbox INSERT + stock mutation + StockMovement.
/// </summary>
public static class ProcurementReceiverEndpoints
{
    private const string LoggerCategory = "SpaceOS.Inventory.ProcurementReceiver";

    public static void MapProcurementReceiverEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/inbound", async (
            ProcurementInboundRequest request,
            HttpContext ctx,
            InventoryDbContext db,
            IConfiguration config,
            IWebHostEnvironment env,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(LoggerCategory);

            // 1. Loopback-only guard (production only — in-memory tests have null RemoteIpAddress)
            if (env.IsProduction())
            {
                var remoteIp = ctx.Connection.RemoteIpAddress;
                if (remoteIp is null || !IPAddress.IsLoopback(remoteIp))
                {
                    logger.LogWarning("ProcurementInbound: rejected non-loopback request from {Ip}", remoteIp);
                    return Results.StatusCode(401);
                }
            }

            // 2. Bearer auth — constant-time compare (SEC-P-01)
            var secret = config["SPACEOS_INTERNAL_SECRET"]
                ?? Environment.GetEnvironmentVariable("SPACEOS_INTERNAL_SECRET");

            if (!ValidateBearer(ctx.Request.Headers.Authorization.FirstOrDefault(), secret))
            {
                logger.LogWarning("ProcurementInbound: invalid or missing Bearer token");
                return Results.StatusCode(401);
            }

            // 3. X-SpaceOS-TenantId header required
            var tenantHeader = ctx.Request.Headers["X-SpaceOS-TenantId"].FirstOrDefault();
            if (!Guid.TryParse(tenantHeader, out var headerTenantId))
            {
                logger.LogWarning("ProcurementInbound: missing or invalid X-SpaceOS-TenantId header");
                return Results.StatusCode(403);
            }

            // 4. Header vs body TenantId strict equal (SEC-P-01)
            if (headerTenantId != request.TenantId)
            {
                logger.LogWarning(
                    "ProcurementInbound: TenantId mismatch — header={HeaderTenant}, body={BodyTenant}",
                    headerTenantId, request.TenantId);
                return Results.StatusCode(403);
            }

            // 5. Validate materialCode exists in catalog (422 = orphan, permanent error for peer)
            var catalog = await db.MaterialCatalogs
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialType == request.MaterialCode, ct)
                .ConfigureAwait(false);
            if (catalog is null)
            {
                logger.LogWarning("ProcurementInbound: unknown materialCode={MaterialCode}", request.MaterialCode);
                return Results.UnprocessableEntity(new
                {
                    error = "Unprocessable",
                    message = $"Unknown materialCode: {request.MaterialCode}"
                });
            }

            // 6. One DB-tx: idempotency check + stock mutation + StockMovement + inbox INSERT
            if (db.Database.IsRelational())
                await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
            try
            {
                if (db.Database.IsRelational())
                    await db.Database.ExecuteSqlRawAsync(
                        "SELECT set_config('app.current_tenant_id', {0}, false)",
                        request.TenantId.ToString()).ConfigureAwait(false);

                // Idempotency: duplicate → 200 (not 409, not 5xx — BE-P-08)
                var alreadyProcessed = await db.InventoryInboundInboxes
                    .AnyAsync(x => x.TenantId == request.TenantId && x.DeliveryLineId == request.DeliveryLineId, ct)
                    .ConfigureAwait(false);

                if (alreadyProcessed)
                {
                    logger.LogInformation(
                        "ProcurementInbound: duplicate DeliveryLineId={DeliveryLineId} for tenant {TenantId} — 200 idempotent",
                        request.DeliveryLineId, request.TenantId);
                    return Results.Ok(new { processed = false, reason = "duplicate" });
                }

                // Stock mutation: find or create PanelStock
                var panelCount = (int)Math.Max(1, Math.Round(request.Quantity));
                var existingStock = await db.PanelStocks
                    .FirstOrDefaultAsync(
                        s => s.TenantId == request.TenantId && s.MaterialCatalogId == catalog.Id,
                        ct)
                    .ConfigureAwait(false);

                if (existingStock is not null)
                {
                    existingStock.AddQuantity(panelCount);
                }
                else
                {
                    var newStock = PanelStock.Create(
                        request.TenantId, catalog.Id,
                        catalog.StandardWidth, catalog.StandardHeight,
                        StockType.FullPanel, panelCount, "PROCUREMENT");
                    await db.PanelStocks.AddAsync(newStock, ct).ConfigureAwait(false);
                }

                // Append-only movement record
                var movement = StockMovement.Record(
                    request.TenantId,
                    MovementType.Inbound,
                    catalog.Id,
                    request.Quantity,
                    request.ReceivedAt.UtcDateTime,
                    $"PROC:{request.SupplierId}:{request.DeliveryLineId}");
                await db.StockMovements.AddAsync(movement, ct).ConfigureAwait(false);

                // Inbox INSERT — idempotency record (same tx)
                var inbox = InventoryInboundInbox.Create(
                    request.TenantId,
                    request.DeliveryLineId,
                    request.MaterialCode,
                    request.Quantity,
                    request.UnitOfMeasure,
                    request.SupplierId,
                    request.ReceivedAt);
                await db.InventoryInboundInboxes.AddAsync(inbox, ct).ConfigureAwait(false);

                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                logger.LogInformation(
                    "ProcurementInbound: processed DeliveryLineId={DeliveryLineId} for tenant {TenantId}, qty={Quantity} {Uom}",
                    request.DeliveryLineId, request.TenantId, request.Quantity, request.UnitOfMeasure);

                return Results.Ok(new { processed = true });
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "ProcurementInbound: transient DB error for DeliveryLineId={DeliveryLineId}", request.DeliveryLineId);
                return Results.StatusCode(503);
            }
            finally
            {
                if (db.Database.IsRelational())
                    await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        })
        .AllowAnonymous()
        .WithTags("Internal");
    }

    // Constant-time Bearer token validation (SEC-P-01)
    private static bool ValidateBearer(string? authHeader, string? secret)
    {
        if (string.IsNullOrEmpty(secret)) return false;
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..].Trim();
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        if (tokenBytes.Length != secretBytes.Length)
        {
            // Still run FixedTimeEquals with same-length buffers to avoid timing leak on length
            CryptographicOperations.FixedTimeEquals(tokenBytes, tokenBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(tokenBytes, secretBytes);
    }
}

public sealed record ProcurementInboundRequest(
    Guid TenantId,
    Guid DeliveryLineId,
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    Guid SupplierId,
    DateTimeOffset ReceivedAt);
