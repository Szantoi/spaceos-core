// SpaceOS.Kernel.Api/Endpoints/SpaceLayerEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Api.Middleware;
using SpaceOS.Kernel.Application.SpaceLayers;
using SpaceOS.Kernel.Application.SpaceLayers.Commands;
using SpaceOS.Kernel.Application.SpaceLayers.Queries;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers SpaceLayer-related Minimal API endpoints (reads and writes).</summary>
public static class SpaceLayerEndpoints
{
    /// <summary>Maps all SpaceLayer GET and write endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapSpaceLayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/space-layers").WithTags("SpaceLayers");

        // --- GET ---

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSpaceLayerByIdQuery(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetSpaceLayerById")
        .WithSummary("Get a space layer by ID")
        .WithDescription("Returns a single space layer by its unique identifier. Returns 404 if not found.")
        .Produces<SpaceLayerDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        // --- PUT ---

        group.MapPut("/{id:guid}/intent", async (Guid id, UpdateSpaceLayerIntentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new UpdateSpaceLayerIntentDataCommand(id, request.IntentDataJson, request.TradeType), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .AddEndpointFilter<RequestBodySizeLimitFilter>()
        .WithName("UpdateSpaceLayerIntent")
        .WithSummary("Update a space layer's intent data")
        .WithDescription("Replaces the local intent data JSON for an existing space layer. Requires WritePolicy. Returns 404 if not found.")
        .Accepts<UpdateSpaceLayerIntentRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(413)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- DELETE ---

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ArchiveSpaceLayerCommand(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ArchiveSpaceLayer")
        .WithSummary("Archive a space layer")
        .WithDescription("Soft-deletes a space layer by setting IsArchived = true. Returns 204 on success, 404 if not found, 409 if already archived.")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("sliding");

        return app;
    }
}

/// <summary>Request body for updating a SpaceLayer's local intent data.</summary>
/// <param name="IntentDataJson">The new intent data JSON string.</param>
/// <param name="TradeType">
/// Optional trade type used to select the per-trade structural JSON schema for validation.
/// When omitted, generic rules apply (max depth 10, max 64 KB, must be an object or array).
/// </param>
public sealed record UpdateSpaceLayerIntentRequest(string IntentDataJson, TradeType? TradeType = null);
