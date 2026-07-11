using MediatR;
using SpaceOS.Modules.Inventory.Application.Events;

namespace SpaceOS.Modules.Inventory.Api.Endpoints;

/// <summary>
/// Integration endpoints — callable by other SpaceOS services via X-Internal-Service header.
/// These endpoints bridge cross-service domain events (fire-and-forget with 202 Accepted).
/// </summary>
public static class IntegrationEndpoints
{
    private const string InternalServiceHeader = "X-Internal-Service";

    public static void MapInventoryIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/inventory/integration/cutting-job-completed
        // Called by CUTTING service when a CuttingJob transitions to "Cut" status.
        app.MapPost("/api/inventory/integration/cutting-job-completed", async (
            CuttingJobCompletedRequest request,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            // SEC: only internal services may call this endpoint
            if (!ctx.Request.Headers.ContainsKey(InternalServiceHeader))
                return Results.StatusCode(403);

            // Dispatch the integration event; handler creates offcut record
            // For v1: materialCatalogId and dimensions use defaults — real values deferred to v1.5
            var notification = new CuttingJobCompletedEvent(
                JobId: request.JobId,
                MaterialCatalogId: Guid.Empty,  // v1 stub — real catalog lookup in v1.5
                MaterialCode: "UNKNOWN",          // v1 stub
                WidthMm: 0m,                      // v1 stub — waste tracked via WasteM2
                HeightMm: 0m,                     // v1 stub
                ThicknessMm: 0m,                  // v1 stub
                WastePercent: request.YieldPct > 0m ? (1m - request.YieldPct / 100m) : 0m,
                TenantId: request.TenantId);

            await mediator.Publish(notification, ct).ConfigureAwait(false);

            return Results.Accepted();
        })
        .AllowAnonymous()  // Auth is header-based (X-Internal-Service), not JWT
        .WithTags("Integration");
    }
}

public sealed record CuttingJobCompletedRequest(
    Guid JobId,
    Guid TenantId,
    Guid CuttingSheetId,
    DateTime CompletedAt,
    decimal YieldPct,
    decimal WasteM2);
