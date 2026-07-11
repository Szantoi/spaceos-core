// SpaceOS.Kernel.Api/Endpoints/FlowEpicEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.FlowEpics.Commands;
using SpaceOS.Kernel.Application.FlowEpics.Commands.CloseFlowEpic;
using SpaceOS.Kernel.Application.FlowEpics.Commands.DelegateFlowEpic;
using SpaceOS.Kernel.Application.FlowEpics.Commands.UploadProof;
using SpaceOS.Kernel.Application.FlowEpics.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers FlowEpic-related Minimal API endpoints (reads and writes).</summary>
public static class FlowEpicEndpoints
{
    /// <summary>Maps all FlowEpic GET and write endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapFlowEpicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/flow-epics").WithTags("FlowEpics");

        // --- GET ---

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFlowEpicByIdQuery(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetFlowEpicById")
        .WithSummary("Get a flow epic by ID")
        .WithDescription("Returns a single flow epic by its unique identifier. Returns 404 if not found.")
        .Produces<FlowEpicDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        // --- PUT ---

        group.MapPut("/{id:guid}/title", async (Guid id, UpdateFlowEpicTitleRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new UpdateFlowEpicTitleCommand(id, request.Title), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UpdateFlowEpicTitle")
        .WithSummary("Update a flow epic's title")
        .WithDescription("Replaces the title of an existing flow epic. Requires WritePolicy. Returns 404 if not found.")
        .Accepts<UpdateFlowEpicTitleRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPut("/{id:guid}/start", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new StartFlowEpicExecutionCommand(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("StartFlowEpicExecution")
        .WithSummary("Start execution of a flow epic")
        .WithDescription("Transitions the flow epic into the executing state. Requires WritePolicy. Returns 404 if not found.")
        .Produces(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPut("/{id:guid}/delegate", async (Guid id, DelegateFlowEpicRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new DelegateFlowEpicCommand(id, request.GuestTenantId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("DelegateFlowEpic")
        .WithSummary("Delegate a flow epic to a guest tenant")
        .WithDescription("Initiates a B2B handshake by delegating the flow epic to the specified guest tenant. Requires WritePolicy. Returns 404 if not found.")
        .Accepts<DelegateFlowEpicRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPut("/{id:guid}/close", async (Guid id, CloseFlowEpicRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new CloseFlowEpicCommand(id, request.ProofUrl, request.ProofHash), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("CloseFlowEpic")
        .WithSummary("Close a flow epic with a verified proof document")
        .WithDescription("Transitions the flow epic from Delivery to ClosedDone. Requires WritePolicy. Returns 404 if not found, 422 if validation fails.")
        .Accepts<CloseFlowEpicRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        group.MapPost("/{id:guid}/proof", async (Guid id, HttpRequest httpRequest, IMediator mediator, CancellationToken ct) =>
        {
            using var buffer = new MemoryStream();
            await httpRequest.Body.CopyToAsync(buffer, ct).ConfigureAwait(false);
            var content = buffer.ToArray();

            var fileName = $"proof/{id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var result = await mediator
                .Send(new UploadFlowEpicProofCommand(id, fileName, content), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UploadFlowEpicProof")
        .WithSummary("Upload a proof document for a flow epic")
        .WithDescription("Stores the proof document in immutable storage and returns the URL and SHA-256 hash. Does not close the epic. Requires WritePolicy.")
        .Accepts<byte[]>("application/octet-stream")
        .Produces<ProofUploadDto>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- DELETE ---

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ArchiveFlowEpicCommand(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ArchiveFlowEpic")
        .WithSummary("Archive a flow epic")
        .WithDescription("Soft-deletes a flow epic by setting IsArchived = true. Returns 204 on success, 404 if not found, 409 if already archived.")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("sliding");

        return app;
    }
}

/// <summary>Request body for updating a FlowEpic's title.</summary>
/// <param name="Title">The new title for the epic.</param>
public sealed record UpdateFlowEpicTitleRequest(string Title);

/// <summary>Request body for delegating a FlowEpic to a guest tenant (B2B handshake).</summary>
/// <param name="GuestTenantId">The identifier of the guest tenant receiving the delegation.</param>
public sealed record DelegateFlowEpicRequest(Guid GuestTenantId);

/// <summary>Request body for closing a FlowEpic with a verified proof document.</summary>
/// <param name="ProofUrl">The URL of the uploaded proof document.</param>
/// <param name="ProofHash">The lowercase hex-encoded SHA-256 hash of the proof document content.</param>
public sealed record CloseFlowEpicRequest(string ProofUrl, string ProofHash);
