// SpaceOS.Kernel.Api/Endpoints/NodeEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Nodes;
using SpaceOS.Kernel.Application.Nodes.Commands.Heartbeat;
using SpaceOS.Kernel.Application.Nodes.Commands.RegisterNode;
using SpaceOS.Kernel.Application.Nodes.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Request body for registering a remote node.</summary>
public sealed record RegisterNodeRequest(Guid TenantId, string ServerUrl);

/// <summary>Request body for recording a node heartbeat.</summary>
public sealed record HeartbeatRequest(Guid TenantId);

/// <summary>Registers node-management Minimal API endpoints.</summary>
public static class NodeEndpoints
{
    /// <summary>Maps all node management endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/nodes").WithTags("Nodes");

        group.MapPost("/register", async (RegisterNodeRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new RegisterNodeCommand(request.TenantId, request.ServerUrl), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("RegisterNode")
        .WithSummary("Register a remote SpaceOS node")
        .WithDescription("Registers a new federation node for a tenant, issues a node JWT, and returns the manifest. Requires AdminPolicy.")
        .Accepts<RegisterNodeRequest>("application/json")
        .Produces<NodeManifestDto>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("node-register");

        group.MapPut("/heartbeat", async (HeartbeatRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new HeartbeatCommand(request.TenantId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("NodeHeartbeat")
        .WithSummary("Record a node heartbeat")
        .WithDescription("Updates the LastHeartbeatAt timestamp for the node registered under the given tenant. Requires WritePolicy.")
        .Accepts<HeartbeatRequest>("application/json")
        .Produces(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("node-heartbeat");

        group.MapGet("/{tenantId:guid}/manifest", async (Guid tenantId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new GetManifestQuery(tenantId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetNodeManifest")
        .WithSummary("Get the node manifest for a tenant")
        .WithDescription("Returns the federation manifest for the node registered under the given tenant. Returns 404 if no manifest exists.")
        .Produces<NodeManifestDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}
