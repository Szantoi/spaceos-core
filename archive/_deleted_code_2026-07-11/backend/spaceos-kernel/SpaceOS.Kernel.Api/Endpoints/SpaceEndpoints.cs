// SpaceOS.Kernel.Api/Endpoints/SpaceEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Spaces.Commands;
using SpaceOS.Kernel.Application.Spaces.Queries;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Spatial BIM Core endpoints for physical spaces, elements, links, and timeline queries.</summary>
public static class SpaceEndpoints
{
    /// <summary>Maps all Space-related Minimal API endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapSpaceEndpoints(this IEndpointRouteBuilder app)
    {
        var spaces = app.MapGroup("/api/spaces").WithTags("Spaces");
        var elements = app.MapGroup("/api/elements").WithTags("Spaces");

        // --- POST /api/spaces ---

        spaces.MapPost("/", async (RegisterPhysicalSpaceRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegisterPhysicalSpaceCommand(
                request.FacilityId,
                request.WidthMm, request.HeightMm, request.DepthMm,
                request.OriginX, request.OriginY, request.OriginZ,
                request.SpaceType,
                request.CellSizeMm), ct).ConfigureAwait(false);
            return result.ToCreatedResult("GetSpaceTimeline", id => new { id });
        })
        .WithName("RegisterPhysicalSpace")
        .WithSummary("Register a new physical space")
        .WithDescription("Creates a new physical space within a facility. Returns the new space identifier.")
        .Accepts<RegisterPhysicalSpaceRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- POST /api/spaces/{id}/elements ---

        spaces.MapPost("/{id:guid}/elements", async (
            Guid id, RegisterSpatialElementRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegisterSpatialElementCommand(
                id,
                request.FlowEpicId,
                request.TradeType,
                request.ElementType,
                request.MinX, request.MinY, request.MinZ,
                request.MaxX, request.MaxY, request.MaxZ), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("RegisterSpatialElement")
        .WithSummary("Register a spatial element in a physical space")
        .WithDescription("Inserts a new spatial element into the BVH tree of the specified physical space.")
        .Accepts<RegisterSpatialElementRequest>("application/json")
        .Produces<Guid>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- POST /api/elements/{id}/links ---

        elements.MapPost("/{id:guid}/links", async (
            Guid id, LinkTaskToElementRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LinkTaskToElementCommand(
                request.FlowTaskId,
                id,
                request.WorkPhase), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("LinkTaskToElement")
        .WithSummary("Link a FlowTask to a spatial element")
        .WithDescription("Creates a link between a FlowTask and a SpatialElement with a specific work phase. SEC-P3A-02: cross-tenant check enforced.")
        .Accepts<LinkTaskToElementRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- GET /api/spaces/{id}/timeline ---

        spaces.MapGet("/{id:guid}/timeline", async (
            Guid id,
            DateTimeOffset? at,
            int page = 1,
            int pageSize = 50,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var atValue = at ?? DateTimeOffset.UtcNow;
            var result = await mediator.Send(new GetSpatialSnapshotAtTQuery(
                id, atValue, page, pageSize), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetSpaceTimeline")
        .WithSummary("Get spatial snapshot at a point in time")
        .WithDescription("Returns a paginated snapshot of spatial elements with their FSM state at the given point in time.")
        .Produces<PagedList<SpatialContractDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        // --- GET /api/spaces/{id}/timeline/events ---

        spaces.MapGet("/{id:guid}/timeline/events", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSpatialTimelineEventsQuery(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetSpaceTimelineEvents")
        .WithSummary("Get spatial timeline events")
        .WithDescription("Returns all timeline events for a physical space in chronological order.")
        .Produces<List<SpatialTimelineEventDto>>(200)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}

/// <summary>Request body for registering a new physical space.</summary>
/// <param name="FacilityId">The facility this space belongs to.</param>
/// <param name="WidthMm">Width in millimetres.</param>
/// <param name="HeightMm">Height in millimetres.</param>
/// <param name="DepthMm">Depth in millimetres.</param>
/// <param name="OriginX">Origin X coordinate in millimetres.</param>
/// <param name="OriginY">Origin Y coordinate in millimetres.</param>
/// <param name="OriginZ">Origin Z coordinate in millimetres.</param>
/// <param name="SpaceType">The classification of the space.</param>
/// <param name="CellSizeMm">The spatial grid cell size in millimetres (default 500).</param>
public sealed record RegisterPhysicalSpaceRequest(
    Guid FacilityId,
    int WidthMm, int HeightMm, int DepthMm,
    int OriginX, int OriginY, int OriginZ,
    SpaceType SpaceType,
    int CellSizeMm = 500);

/// <summary>Request body for registering a spatial element within a physical space.</summary>
/// <param name="FlowEpicId">The FlowEpic this element belongs to.</param>
/// <param name="TradeType">The construction trade type.</param>
/// <param name="ElementType">The driver-specific element classification.</param>
/// <param name="MinX">Minimum X coordinate in millimetres.</param>
/// <param name="MinY">Minimum Y coordinate in millimetres.</param>
/// <param name="MinZ">Minimum Z coordinate in millimetres.</param>
/// <param name="MaxX">Maximum X coordinate in millimetres.</param>
/// <param name="MaxY">Maximum Y coordinate in millimetres.</param>
/// <param name="MaxZ">Maximum Z coordinate in millimetres.</param>
public sealed record RegisterSpatialElementRequest(
    Guid FlowEpicId,
    TradeType TradeType,
    string ElementType,
    int MinX, int MinY, int MinZ,
    int MaxX, int MaxY, int MaxZ);

/// <summary>Request body for linking a FlowTask to a spatial element.</summary>
/// <param name="FlowTaskId">The FlowTask identifier to link.</param>
/// <param name="WorkPhase">The manufacturing or installation phase.</param>
public sealed record LinkTaskToElementRequest(
    Guid FlowTaskId,
    WorkPhase WorkPhase);
