// SpaceOS.Kernel.Api/Endpoints/FacilityEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Facilities.Commands;
using SpaceOS.Kernel.Application.Facilities.Queries;
using SpaceOS.Kernel.Application.FlowEpics.Commands;
using SpaceOS.Kernel.Application.FlowEpics.Queries;
using SpaceOS.Kernel.Application.SpaceLayers;
using SpaceOS.Kernel.Application.SpaceLayers.Commands;
using SpaceOS.Kernel.Application.SpaceLayers.Queries;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Application.WorkStations.Commands;
using SpaceOS.Kernel.Application.WorkStations.Queries;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Facility-related Minimal API endpoints (reads and writes), including nested sub-resource routes.</summary>
public static class FacilityEndpoints
{
    /// <summary>Maps all Facility GET and write endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapFacilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/facilities").WithTags("Facilities");

        // --- GET ---

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFacilityByIdQuery(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetFacilityById")
        .WithSummary("Get a facility by ID")
        .WithDescription("Returns a single facility by its unique identifier. Returns 404 if not found.")
        .Produces<FacilityDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{facilityId:guid}/work-stations", async (
            Guid facilityId,
            int page = 1,
            int pageSize = 20,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetWorkStationsByFacilityQuery(facilityId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetWorkStationsByFacility")
        .WithSummary("List work stations for a facility (paged)")
        .WithDescription("Returns a paged list of work stations belonging to the specified facility. Returns 404 if the facility does not exist.")
        .Produces<PagedList<WorkStationDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{facilityId:guid}/space-layers", async (
            Guid facilityId,
            int page = 1,
            int pageSize = 20,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetSpaceLayersByFacilityQuery(facilityId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetSpaceLayersByFacility")
        .WithSummary("List space layers for a facility (paged)")
        .WithDescription("Returns a paged list of space layers belonging to the specified facility. Returns 404 if the facility does not exist.")
        .Produces<PagedList<SpaceLayerDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{facilityId:guid}/flow-epics", async (
            Guid facilityId,
            int page = 1,
            int pageSize = 20,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetFlowEpicsByFacilityQuery(facilityId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetFlowEpicsByFacility")
        .WithSummary("List flow epics for a facility (paged)")
        .WithDescription("Returns a paged list of flow epics belonging to the specified facility. Returns 404 if the facility does not exist.")
        .Produces<PagedList<FlowEpicDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        // --- POST (nested creates) ---

        group.MapPost("/{facilityId:guid}/work-stations", async (
            Guid facilityId, RegisterWorkStationRequest request, IMediator mediator,
            ITenantResolver tenantResolver, IFacilityRepository facilityRepo, CancellationToken ct) =>
        {
            var tenantId = await ResolveTenantIdAsync(tenantResolver, facilityRepo, facilityId, ct)
                .ConfigureAwait(false);
            var result = await mediator
                .Send(new RegisterWorkStationCommand(request.Name, request.Type, facilityId, tenantId), ct)
                .ConfigureAwait(false);
            return result.ToCreatedResult("GetWorkStationById", id => new { id });
        })
        .WithName("RegisterWorkStation")
        .WithSummary("Register a work station in a facility")
        .WithDescription("Registers a new work station under the specified facility. Requires WritePolicy. Returns the new work station ID on success.")
        .Accepts<RegisterWorkStationRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .ProducesProblem(500)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPost("/{facilityId:guid}/space-layers", async (
            Guid facilityId, RegisterSpaceLayerRequest request, IMediator mediator,
            ITenantResolver tenantResolver, IFacilityRepository facilityRepo, CancellationToken ct) =>
        {
            var tenantId = await ResolveTenantIdAsync(tenantResolver, facilityRepo, facilityId, ct)
                .ConfigureAwait(false);
            var result = await mediator
                .Send(new RegisterSpaceLayerCommand(
                    facilityId,
                    request.TradeType,
                    request.IsExternalNode,
                    request.ExternalSourceUrl,
                    request.IntentDataJson,
                    tenantId), ct)
                .ConfigureAwait(false);
            return result.ToCreatedResult("GetSpaceLayerById", id => new { id });
        })
        .WithName("RegisterSpaceLayer")
        .WithSummary("Register a space layer in a facility")
        .WithDescription("Registers a new space layer under the specified facility. Requires WritePolicy. Returns the new space layer ID on success.")
        .Accepts<RegisterSpaceLayerRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .ProducesProblem(500)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPost("/{facilityId:guid}/flow-epics", async (
            Guid facilityId, CreateFlowEpicRequest request, IMediator mediator,
            ITenantResolver tenantResolver, IFacilityRepository facilityRepo, CancellationToken ct) =>
        {
            var tenantId = await ResolveTenantIdAsync(tenantResolver, facilityRepo, facilityId, ct)
                .ConfigureAwait(false);
            var result = await mediator
                .Send(new CreateFlowEpicCommand(request.Title, facilityId, tenantId), ct)
                .ConfigureAwait(false);
            return result.ToCreatedResult("GetFlowEpicById", id => new { id });
        })
        .WithName("CreateFlowEpic")
        .WithSummary("Create a flow epic in a facility")
        .WithDescription("Creates a new flow epic under the specified facility. Requires WritePolicy. Returns the new flow epic ID on success.")
        .Accepts<CreateFlowEpicRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .ProducesProblem(500)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- PUT ---

        group.MapPut("/{id:guid}", async (Guid id, RenameFacilityRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new RenameFacilityCommand(id, request.Name), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("RenameFacility")
        .WithSummary("Rename an existing facility")
        .WithDescription("Updates the display name of an existing facility. Requires WritePolicy. Returns 404 if not found.")
        .Accepts<RenameFacilityRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- DELETE ---

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ArchiveFacilityCommand(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ArchiveFacility")
        .WithSummary("Archive a facility")
        .WithDescription("Soft-deletes a facility by setting IsArchived = true. Returns 204 on success, 404 if not found, 409 if already archived.")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("sliding");

        return app;
    }

    /// <summary>
    /// Resolves the tenant ID from the JWT <c>tid</c> claim. When the claim is absent
    /// (e.g. dev tokens), falls back to the owning facility's tenant.
    /// </summary>
    private static async Task<Guid> ResolveTenantIdAsync(
        ITenantResolver tenantResolver,
        IFacilityRepository facilityRepo,
        Guid facilityId,
        CancellationToken ct)
    {
        var resolved = tenantResolver.TryResolve();
        if (resolved is not null)
            return resolved.Value.Value;

        var facility = await facilityRepo
            .GetByIdAsync(FacilityId.From(facilityId), ct)
            .ConfigureAwait(false);

        return facility?.TenantId.Value ?? Guid.Empty;
    }
}

/// <summary>Request body for registering a new WorkStation under a Facility.</summary>
/// <param name="Name">The display name for the workstation.</param>
/// <param name="Type">The type classification string.</param>
public sealed record RegisterWorkStationRequest(string Name, string Type);

/// <summary>Request body for registering a new SpaceLayer under a Facility.</summary>
/// <param name="TradeType">The construction trade this layer represents.</param>
/// <param name="IsExternalNode">Whether this layer is federated (external).</param>
/// <param name="ExternalSourceUrl">URL of the external source (required when federated).</param>
/// <param name="IntentDataJson">Local intent data JSON (used when not federated).</param>
public sealed record RegisterSpaceLayerRequest(
    TradeType TradeType,
    bool IsExternalNode,
    string? ExternalSourceUrl,
    string? IntentDataJson);

/// <summary>Request body for creating a new FlowEpic under a Facility.</summary>
/// <param name="Title">A non-empty title describing the scope of the epic.</param>
public sealed record CreateFlowEpicRequest(string Title);

/// <summary>Request body for renaming an existing Facility.</summary>
/// <param name="Name">The new display name.</param>
public sealed record RenameFacilityRequest(string Name);
