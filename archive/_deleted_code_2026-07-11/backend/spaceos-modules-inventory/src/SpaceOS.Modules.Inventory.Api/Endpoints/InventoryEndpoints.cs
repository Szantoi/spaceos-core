using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;
using SpaceOS.Modules.Inventory.Application.Commands.RecordInbound;
using SpaceOS.Modules.Inventory.Application.Commands.RecordOffcut;
using SpaceOS.Modules.Inventory.Application.Handlers;
using SpaceOS.Modules.Inventory.Application.Queries.GetConsumptionTrend;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcuts;
using SpaceOS.Modules.Inventory.Application.Queries.GetStock;

namespace SpaceOS.Modules.Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .RequireAuthorization("ManufacturerOnly");

        group.MapGet("/stock", GetStock);
        group.MapGet("/offcuts", GetOffcuts);
        group.MapPost("/movements/consumption", RecordConsumption);
        group.MapPost("/movements/inbound", RecordInbound);
        group.MapPost("/movements/offcut", RecordOffcut);
        group.MapGet("/trend", GetConsumptionTrend);

        // Reservations
        group.MapPost("/reservations", ReserveStock);
        group.MapDelete("/reservations/{correlationId:guid}", ReleaseReservation);
        group.MapGet("/reservations", GetReservations);

        return app;
    }

    private static async Task<IResult> GetStock(
        string? materialType,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetStockQuery(materialType ?? "MDF 18mm");
        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
    }

    private static async Task<IResult> GetOffcuts(
        string? materialType,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetOffcutsQuery(materialType ?? string.Empty);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Ok(Array.Empty<object>());
    }

    private static async Task<IResult> RecordConsumption(
        RecordConsumptionRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var command = new RecordConsumptionCommand(tenantId, request.MaterialType, request.Thickness, request.Area, request.PanelCount, request.Reason, request.OccurredAt);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> RecordInbound(
        RecordInboundRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var command = new RecordInboundCommand(tenantId, request.MaterialType, request.Thickness, request.Area, request.PanelCount, request.Reference, request.OccurredAt);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        if (result.IsSuccess) return Results.Created();
        if (result.Status == Ardalis.Result.ResultStatus.NotFound) return Results.NotFound(result.Errors);
        return Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> RecordOffcut(
        RecordOffcutRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var command = new RecordOffcutCommand(tenantId, request.MaterialType, request.WidthMm, request.HeightMm, request.OriginCuttingSheetId);
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> GetConsumptionTrend(
        string? materialType,
        DateTime? from,
        DateTime? to,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetConsumptionTrendQuery(
            materialType ?? "MDF 18mm",
            from ?? DateTime.UtcNow.AddMonths(-1),
            to ?? DateTime.UtcNow);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> ReserveStock(
        ReserveStockRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var command = new ReserveStockCommand(
            tenantId,
            request.CorrelationId,
            request.ConsumerModule,
            request.ConsumerContextJson,
            request.CreatedByUserId,
            request.Items.Select(i => (i.StockItemId, i.MaterialCode, i.Quantity)).ToList(),
            request.Ttl);

        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return Results.BadRequest(result.Errors);
        return Results.Created($"/api/inventory/reservations/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> ReleaseReservation(
        Guid correlationId,
        string? reason,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var result = await mediator
            .Send(new ReleaseReservationCommand(tenantId, correlationId, reason), ct)
            .ConfigureAwait(false);

        return result.IsSuccess ? Results.Ok() : Results.NotFound(result.Errors);
    }

    private static async Task<IResult> GetReservations(
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct,
        string? consumerModule = null,
        Guid? correlationId = null,
        DateTimeOffset? createdAfter = null,
        DateTimeOffset? createdBefore = null,
        int skip = 0,
        int take = 100)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var filter = new ReservationFilter(consumerModule, null, correlationId,
            createdAfter, createdBefore, skip, Math.Clamp(take, 1, 500));

        var result = await mediator
            .Send(new GetReservationsQuery(tenantId, filter), ct)
            .ConfigureAwait(false);

        return Results.Ok(result.Value ?? new List<ReservationDto>());
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tid")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record RecordConsumptionRequest(string MaterialType, decimal Thickness, decimal Area, int PanelCount, string Reason, DateTime OccurredAt);
public sealed record RecordInboundRequest(string MaterialType, decimal Thickness, decimal Area, int PanelCount, string Reference, DateTime OccurredAt);
public sealed record RecordOffcutRequest(string MaterialType, decimal WidthMm, decimal HeightMm, Guid? OriginCuttingSheetId);

public sealed record ReserveStockRequest(
    Guid CorrelationId,
    string ConsumerModule,
    string? ConsumerContextJson,
    Guid? CreatedByUserId,
    IReadOnlyList<ReserveStockItemRequest> Items,
    TimeSpan Ttl);

public sealed record ReserveStockItemRequest(Guid StockItemId, string MaterialCode, decimal Quantity);
