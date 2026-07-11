using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.FinalizeGyartasilap;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateAndStoreGyartasilap;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetBatchStatus;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetGyartasilap;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.ListGyartasilapByOrder;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Services;

public static class GyartasilapEndpoints
{
    public static void MapGyartasilapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gyartasilap")
            .RequireAuthorization("ManufacturerOnly");

        // POST /api/gyartasilap/generate
        group.MapPost("/generate", async (
            [FromBody] GenerateGyartasilapRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var cmd = new GenerateAndStoreGyartasilapCommand(
                tenantId,
                req.JoineryOrderId,
                req.CuttingPlanId,
                req.LabelVariant);

            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Created(
                    $"/api/gyartasilap/{result.Value.GyartasilapId}",
                    result.Value),
                ResultStatus.NotFound => Results.NotFound(result.Errors),
                ResultStatus.Invalid => Results.BadRequest(result.ValidationErrors),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("GenerateGyartasilap")
        .Produces<GenerateAndStoreGyartasilapResponse>(201)
        .ProducesValidationProblem();

        // GET /api/gyartasilap/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var result = await mediator.Send(new GetGyartasilapQuery(tenantId, id), ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Ok(result.Value),
                ResultStatus.NotFound => Results.NotFound(),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("GetGyartasilap")
        .Produces<GetGyartasilapResponse>();

        // PUT /api/gyartasilap/{id}/finalize
        group.MapPut("/{id:guid}/finalize", async (
            Guid id,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var result = await mediator.Send(
                new FinalizeGyartasilapCommand(tenantId, id), ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Ok(new { status = nameof(GyartasilapStatus.Finalized) }),
                ResultStatus.NotFound => Results.NotFound(),
                ResultStatus.Invalid => Results.Conflict(result.ValidationErrors),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("FinalizeGyartasilap");

        // GET /api/gyartasilap/order/{orderId}/list
        group.MapGet("/order/{orderId:guid}/list", async (
            Guid orderId,
            IMediator mediator,
            HttpContext ctx,
            [FromQuery] string? status,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            GyartasilapStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<GyartasilapStatus>(status, ignoreCase: true, out var parsed))
                    return Results.BadRequest($"Invalid status '{status}'. Use: Draft, Finalized, or Archived.");
                statusFilter = parsed;
            }

            var result = await mediator.Send(
                new ListGyartasilapByOrderQuery(tenantId, orderId, statusFilter), ct).ConfigureAwait(false);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        })
        .WithName("ListGyartasilapByOrder")
        .Produces<IReadOnlyList<ListGyartasilapItem>>();

        // POST /api/gyartasilap/batch
        group.MapPost("/batch", async (
            [FromBody] GenerateBatchRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var cmd = new GenerateBatchCommand(tenantId, req.OrderId, req.GyartasilapIds);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Created(
                    $"/api/gyartasilap/batch/{result.Value.BatchId}/status",
                    result.Value),
                ResultStatus.Invalid => Results.BadRequest(result.ValidationErrors),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("GenerateGyartasilapBatch")
        .Produces<GenerateBatchResponse>(201)
        .ProducesValidationProblem();

        // GET /api/gyartasilap/batch/{batchId}/status
        group.MapGet("/batch/{batchId:guid}/status", async (
            Guid batchId,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var result = await mediator.Send(
                new GetBatchStatusQuery(tenantId, batchId), ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Ok(result.Value),
                ResultStatus.NotFound => Results.NotFound(),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("GetGyartasilapBatchStatus")
        .Produces<GetBatchStatusResponse>();

        // GET /api/gyartasilap/batch/{batchId}/download
        group.MapGet("/batch/{batchId:guid}/download", async (
            Guid batchId,
            IMediator mediator,
            IGyartasilapStorage storage,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var statusResult = await mediator.Send(
                new GetBatchStatusQuery(tenantId, batchId), ct).ConfigureAwait(false);

            if (statusResult.Status == ResultStatus.NotFound)
                return Results.NotFound();

            if (!statusResult.IsSuccess)
                return Results.BadRequest(statusResult.Errors);

            var batch = statusResult.Value;
            if (batch.ZipStoragePath is null)
                return Results.BadRequest("Batch is not yet ready for download.");

            var presignedUrl = await storage
                .GetPresignedUrlAsync(batch.ZipStoragePath, ct)
                .ConfigureAwait(false);

            return Results.Redirect(presignedUrl);
        })
        .WithName("DownloadGyartasilapBatch");
    }

    // SEC-01: Guid.Empty or missing tenant_id claim → null → 401
    private static Guid? TryGetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) && id != Guid.Empty ? id : null;
    }
}
