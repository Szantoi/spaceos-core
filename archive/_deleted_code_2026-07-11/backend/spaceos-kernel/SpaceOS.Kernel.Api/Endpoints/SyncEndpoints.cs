// SpaceOS.Kernel.Api/Endpoints/SyncEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Sync.Commands.ReceiveSignal;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Request body for receiving an offline sync signal.</summary>
public sealed record ReceiveSyncSignalRequest(
    Guid TenantId,
    Guid EpicId,
    string NewState,
    Guid ClientSignalId,
    string PayloadJson);

/// <summary>Registers sync-signal Minimal API endpoints.</summary>
public static class SyncEndpoints
{
    /// <summary>Maps all sync endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync").WithTags("Sync");

        group.MapPost("/signal", async (ReceiveSyncSignalRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new ReceiveSyncSignalCommand(
                    request.TenantId,
                    request.EpicId,
                    request.NewState,
                    request.ClientSignalId,
                    request.PayloadJson), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ReceiveSyncSignal")
        .WithSummary("Receive an offline sync signal")
        .WithDescription("Appends a state-change signal to the tenant's hash chain. Idempotent on duplicate ClientSignalId. Requires WritePolicy.")
        .Accepts<ReceiveSyncSignalRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sync-signal");

        return app;
    }
}
