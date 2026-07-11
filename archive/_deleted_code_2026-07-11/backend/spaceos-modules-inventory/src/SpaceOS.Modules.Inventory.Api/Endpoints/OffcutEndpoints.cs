using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using HttpResult = Microsoft.AspNetCore.Http.IResult;
using SpaceOS.Modules.Inventory.Application.Commands.ApproveOffcutReservation;
using SpaceOS.Modules.Inventory.Application.Commands.RegisterOffcutBatch;
using SpaceOS.Modules.Inventory.Application.Commands.ReserveOffcut;
using SpaceOS.Modules.Inventory.Application.Commands.UseOffcutInJob;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutDetail;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutList;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutStatsSummary;

namespace SpaceOS.Modules.Inventory.Api.Endpoints;

public static class OffcutEndpoints
{
    public static IEndpointRouteBuilder MapOffcutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/offcuts")
            .RequireAuthorization("ManufacturerOnly");

        group.MapGet("/",           GetOffcutList);
        group.MapGet("/stats/summary", GetStatsSummary);
        group.MapGet("/{offcutId:guid}", GetOffcutDetail);
        group.MapPost("/batch",                               RegisterBatch);
        group.MapPost("/{offcutId:guid}/reserve",             ReserveOffcut);
        group.MapPost("/{offcutId:guid}/approve-reservation", ApproveReservation);
        group.MapPost("/{offcutId:guid}/use",                 UseOffcut);

        return app;
    }

    private static async Task<HttpResult> GetOffcutList(
        IMediator mediator,
        CancellationToken ct,
        string? status = null,
        string? materialCode = null,
        decimal? minVolumeM3 = null,
        DateTime? createdAfter = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new GetOffcutListQuery(status, materialCode, minVolumeM3, createdAfter,
            page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize);
        var result = await mediator.Send(query, ct).ConfigureAwait(false);
        return Results.Ok(result.Value);
    }

    private static async Task<HttpResult> GetStatsSummary(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOffcutStatsSummaryQuery(), ct).ConfigureAwait(false);
        return Results.Ok(result.Value);
    }

    private static async Task<HttpResult> GetOffcutDetail(
        Guid offcutId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetOffcutDetailQuery(offcutId), ct).ConfigureAwait(false);
        return result.Status == ResultStatus.NotFound
            ? Results.NotFound(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<HttpResult> ReserveOffcut(
        Guid offcutId,
        ReserveOffcutRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        var result = await mediator
            .Send(new ReserveOffcutCommand(offcutId, request.JobId, tenantId), ct)
            .ConfigureAwait(false);

        return result.Status switch
        {
            ResultStatus.Ok      => Results.Created($"/api/inventory/offcuts/{offcutId}", result.Value),
            ResultStatus.NotFound => Results.NotFound(result.Errors),
            ResultStatus.Conflict => Results.Conflict(result.Errors),
            _                     => Results.BadRequest(result.Errors)
        };
    }

    private static async Task<HttpResult> ApproveReservation(
        Guid offcutId,
        ApproveReservationRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new ApproveOffcutReservationCommand(request.ReservationId), ct)
            .ConfigureAwait(false);

        return result.Status switch
        {
            ResultStatus.Ok       => Results.Ok(result.Value),
            ResultStatus.NotFound  => Results.NotFound(result.Errors),
            ResultStatus.Error when result.Errors.Any(e => e.Contains("expired", StringComparison.OrdinalIgnoreCase))
                                  => Results.Problem(statusCode: 410, detail: "Reservation has expired."),
            _                     => Results.BadRequest(result.Errors)
        };
    }

    private static async Task<HttpResult> UseOffcut(
        Guid offcutId,
        UseOffcutRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new UseOffcutInJobCommand(offcutId, request.JobId), ct)
            .ConfigureAwait(false);

        return result.Status switch
        {
            ResultStatus.Ok       => Results.Ok(result.Value),
            ResultStatus.NotFound  => Results.NotFound(result.Errors),
            ResultStatus.Conflict  => Results.Conflict(result.Errors),
            _                      => Results.BadRequest(result.Errors)
        };
    }

    private static async Task<HttpResult> RegisterBatch(
        RegisterOffcutBatchRequest? request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty) return Results.Unauthorized();

        if (request is null) return Results.BadRequest("Request body is required.");

        if (request.Items is null || request.Items.Count == 0)
            return Results.BadRequest("Items list cannot be empty.");

        var command = new RegisterOffcutBatchCommand(
            tenantId,
            request.SourceType,
            request.SourceId,
            request.Items.Select(i => new OffcutItemDto(
                i.MaterialCatalogId, i.MaterialCode, i.WidthMm, i.HeightMm, i.ThicknessMm)).ToList());

        var result = await mediator.Send(command, ct).ConfigureAwait(false);

        return result.Status switch
        {
            ResultStatus.Ok when result.Value.IsNew  => Results.Created("/api/inventory/offcuts/batch", result.Value),
            ResultStatus.Ok                           => Results.Ok(result.Value),
            _                                         => Results.BadRequest(result.Errors)
        };
    }

    private static Guid GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tid")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record ReserveOffcutRequest(Guid JobId);
public sealed record ApproveReservationRequest(Guid ReservationId);
public sealed record UseOffcutRequest(Guid JobId);
public sealed record RegisterOffcutBatchRequest(
    string SourceType,
    Guid SourceId,
    IReadOnlyList<OffcutItemRequest> Items
);
public sealed record OffcutItemRequest(
    Guid MaterialCatalogId,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm
);
