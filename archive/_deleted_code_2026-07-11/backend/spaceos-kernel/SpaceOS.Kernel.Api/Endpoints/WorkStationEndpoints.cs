// SpaceOS.Kernel.Api/Endpoints/WorkStationEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Application.WorkStations.Commands;
using SpaceOS.Kernel.Application.WorkStations.Queries;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers WorkStation-related Minimal API endpoints (reads and writes).</summary>
public static class WorkStationEndpoints
{
    /// <summary>Maps all WorkStation GET and write endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapWorkStationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-stations").WithTags("WorkStations");

        // --- GET ---

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWorkStationByIdQuery(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetWorkStationById")
        .WithSummary("Get a work station by ID")
        .WithDescription("Returns a single work station by its unique identifier. Returns 404 if not found.")
        .Produces<WorkStationDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        // --- PUT ---

        group.MapPut("/{id:guid}", async (Guid id, UpdateWorkStationNameRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new UpdateWorkStationNameCommand(id, request.Name), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UpdateWorkStationName")
        .WithSummary("Update a work station's display name")
        .WithDescription("Renames an existing work station. Requires WritePolicy. Returns 404 if the work station does not exist.")
        .Accepts<UpdateWorkStationNameRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPut("/{id:guid}/status", async (Guid id, UpdateWorkStationStatusRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new UpdateWorkStationStatusCommand(id, request.Status), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UpdateWorkStationStatus")
        .WithSummary("Update a work station's operational status")
        .WithDescription("Changes the operational status of an existing work station. Requires WritePolicy. Returns 404 if not found.")
        .Accepts<UpdateWorkStationStatusRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPut("/{id:guid}/facility", async (Guid id, AssignWorkStationToFacilityRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new AssignWorkStationToFacilityCommand(id, request.FacilityId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("AssignWorkStationToFacility")
        .WithSummary("Reassign a work station to a facility")
        .WithDescription("Moves a work station to a different facility. Requires WritePolicy. Returns 404 if the work station or target facility does not exist.")
        .Accepts<AssignWorkStationToFacilityRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- DELETE ---

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ArchiveWorkStationCommand(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ArchiveWorkStation")
        .WithSummary("Archive a work station")
        .WithDescription("Soft-deletes a work station by setting IsArchived = true. Returns 204 on success, 404 if not found, 409 if already archived.")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("sliding");

        return app;
    }
}

/// <summary>Request body for updating a WorkStation's display name.</summary>
/// <param name="Name">The new display name.</param>
public sealed record UpdateWorkStationNameRequest(string Name);

/// <summary>Request body for updating a WorkStation's operational status.</summary>
/// <param name="Status">The desired new status.</param>
public sealed record UpdateWorkStationStatusRequest(WorkStationStatus Status);

/// <summary>Request body for assigning a WorkStation to a different Facility.</summary>
/// <param name="FacilityId">The target facility identifier.</param>
public sealed record AssignWorkStationToFacilityRequest(Guid FacilityId);
