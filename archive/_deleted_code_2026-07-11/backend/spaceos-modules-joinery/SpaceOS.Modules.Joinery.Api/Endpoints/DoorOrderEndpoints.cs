using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.AddDoorItem;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.CalculateDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.RevertDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.SaveCalculationResult;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.SubmitDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareList;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialRequirements;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProcessPlan;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareListPdf;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetManufacturingSheet;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialReqPdf;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProductionSheet;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetSnapshots;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.ListDoorOrders;

public static class DoorOrderEndpoints
{
    public static void MapDoorOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .RequireAuthorization("ManufacturerOnly");

        // POST /api/orders
        group.MapPost("/", async (
            [FromBody] CreateDoorOrderRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var cmd = new CreateDoorOrderCommand(tenantId, req.FlowEpicId, req.ProjectId, req.ProjectName,
                req.ClientName, req.ClientAddress, req.ClientPhone, req.DeliveryDate);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/api/orders/{result.Value}", result.Value)
                : Results.BadRequest(result.Errors);
        });

        // POST /api/orders/{id}/items
        group.MapPost("/{id:guid}/items", async (
            Guid id, [FromBody] AddDoorItemRequest req,
            IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var cmd = new AddDoorItemCommand(tenantId, id, req.Sorszam, req.Name, req.Quantity,
                req.DoorType, req.OpeningDirection,
                req.WallOpeningWidth, req.DoorWidth, req.WallOpeningHeight,
                req.DoorHeight, req.WallOpeningThickness, req.DoorThickness);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/api/orders/{id}/items/{result.Value}", result.Value)
                : Results.BadRequest(result.Errors);
        });

        // POST /api/orders/{id}/calculate
        group.MapPost("/{id:guid}/calculate", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new CalculateDoorOrderCommand(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // GET /api/orders/{id}/cutting-list (SEC-05: Cache-Control: no-store)
        group.MapGet("/{id:guid}/cutting-list", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetCuttingListQuery(tenantId, id), ct).ConfigureAwait(false);
            if (!result.IsSuccess) return Results.NotFound();
            ctx.Response.Headers.CacheControl = "no-store";
            return Results.Ok(result.Value);
        });

        // GET /api/orders/{id}/process-plan
        group.MapGet("/{id:guid}/process-plan", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetProcessPlanQuery(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // GET /api/orders/{id}/hardware-list
        group.MapGet("/{id:guid}/hardware-list", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetHardwareListQuery(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // GET /api/orders/{id}/material-req
        group.MapGet("/{id:guid}/material-req", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetMaterialRequirementsQuery(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // POST /api/orders/{id}/submit
        group.MapPost("/{id:guid}/submit", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new SubmitDoorOrderCommand(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });

        // GET /api/orders
        group.MapGet("/", async (
            IMediator mediator, HttpContext ctx,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var clampedPageSize = Math.Clamp(pageSize, 1, 100);
            var result = await mediator.Send(new ListDoorOrdersQuery(tenantId, page, clampedPageSize), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // GET /api/orders/{id}
        group.MapGet("/{id:guid}", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetDoorOrderQuery(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // GET /api/orders/{id}/sheet — PDF production sheet (SEC-05)
        group.MapGet("/{id:guid}/sheet", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetProductionSheetQuery(id, tenantId), ct).ConfigureAwait(false);
            if (!result.IsSuccess) return result.Status == Ardalis.Result.ResultStatus.NotFound
                ? Results.NotFound()
                : Results.BadRequest(result.Errors);

            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["Cache-Control"] = "private, no-store";

            return Results.File(result.Value, "application/pdf",
                fileDownloadName: $"gyartasilap_{id:N}.pdf",
                enableRangeProcessing: false);
        })
        .WithName("GetProductionSheet")
        .Produces(200, contentType: "application/pdf");

        // GET /api/orders/{id}/hardware-list-pdf — Hardverlista PDF
        group.MapGet("/{id:guid}/hardware-list-pdf", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetHardwareListPdfQuery(id, tenantId), ct).ConfigureAwait(false);
            if (!result.IsSuccess) return Results.NotFound();

            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["Cache-Control"] = "private, no-store";

            return Results.File(result.Value, "application/pdf",
                fileDownloadName: $"hardverlista_{id:N}.pdf",
                enableRangeProcessing: false);
        })
        .WithName("GetHardwareListPdf")
        .Produces(200, contentType: "application/pdf");

        // GET /api/orders/{id}/material-req-pdf — Anyagnorma PDF
        group.MapGet("/{id:guid}/material-req-pdf", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetMaterialReqPdfQuery(id, tenantId), ct).ConfigureAwait(false);
            if (!result.IsSuccess) return Results.NotFound();

            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["Cache-Control"] = "private, no-store";

            return Results.File(result.Value, "application/pdf",
                fileDownloadName: $"anyagnorma_{id:N}.pdf",
                enableRangeProcessing: false);
        })
        .WithName("GetMaterialReqPdf")
        .Produces(200, contentType: "application/pdf");

        // GET /api/orders/{id}/manufacturing-sheet — Gyártásilap PDF (DoorItems alapú, minden státuszra)
        group.MapGet("/{id:guid}/manufacturing-sheet", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetManufacturingSheetQuery(id, tenantId), ct).ConfigureAwait(false);
            if (!result.IsSuccess) return Results.NotFound();

            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["Cache-Control"] = "private, no-store";

            return Results.File(result.Value, "application/pdf",
                fileDownloadName: $"gyartasilap_{id:N}.pdf",
                enableRangeProcessing: false);
        })
        .WithName("GetManufacturingSheet")
        .Produces(200, contentType: "application/pdf");

        // GET /api/orders/{id}/snapshots
        group.MapGet("/{id:guid}/snapshots", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new GetSnapshotsQuery(id, tenantId), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        })
        .WithName("GetSnapshots");

        // PUT /api/orders/{id}/revert
        group.MapPut("/{id:guid}/revert", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();
            var result = await mediator.Send(new RevertDoorOrderCommand(tenantId, id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });

        // PUT /api/orders/internal/results
        // Called by the Orchestrator via internal proxy; protected by ManufacturerOnly policy.
        group.MapPut("/internal/results", async (
            [FromBody] SaveCalculationResultCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });
    }

    // SEC-01: Guid.Empty vagy hiányzó tenant_id claim → null → 401
    private static Guid? TryGetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) && id != Guid.Empty ? id : null;
    }
}
