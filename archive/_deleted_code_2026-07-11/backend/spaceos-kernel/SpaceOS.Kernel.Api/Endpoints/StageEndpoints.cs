// SpaceOS.Kernel.Api/Endpoints/StageEndpoints.cs
using System;
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.StageRegistry;
using SpaceOS.Kernel.Application.StageRegistry.Commands;
using SpaceOS.Kernel.Application.StageRegistry.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Stage Registry Minimal API endpoints (SEC-02).</summary>
public static class StageEndpoints
{
    /// <summary>Maps all Stage Registry endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapStageEndpoints(this IEndpointRouteBuilder app)
    {
        MapStageDefinitionEndpoints(app);
        MapStageChainEndpoints(app);
        MapStageHandoffEndpoints(app);
        MapFlowEpicStageControlEndpoints(app);
        return app;
    }

    // ─── Stage Registry — SystemAdmin / TenantUser ──────────────────────────────

    private static void MapStageDefinitionEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stages").WithTags("StageRegistry");

        group.MapPost("/", async (RegisterStageDefinitionRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new RegisterStageDefinitionCommand(req.TenantId, req.StageCode, req.DisplayName, req.ModuleEndpoint),
                ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("RegisterStageDefinition")
        .WithSummary("Register a stage definition")
        .WithDescription("Registers a new Stage Module definition for a tenant. Requires SystemAdmin role.")
        .Accepts<RegisterStageDefinitionRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(403)
        .RequireAuthorization("SystemAdminPolicy")
        .RequireRateLimiting("sliding");

        group.MapGet("/", async (IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListStageDefinitionsQuery(), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ListStageDefinitions")
        .WithSummary("List active stage definitions")
        .WithDescription("Returns all active stage definitions for the current tenant.")
        .Produces<System.Collections.Generic.IReadOnlyList<StageDefinitionDto>>(200)
        .ProducesProblem(403)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapPut("/{id:guid}", async (Guid id, UpdateStageDefinitionRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateStageDefinitionCommand(id, req.ModuleEndpoint), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UpdateStageDefinition")
        .WithSummary("Update a stage definition endpoint")
        .WithDescription("Replaces the module endpoint URL of an existing stage definition. Requires SystemAdmin role.")
        .Accepts<UpdateStageDefinitionRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("SystemAdminPolicy")
        .RequireRateLimiting("sliding");

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeactivateStageDefinitionCommand(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("DeactivateStageDefinition")
        .WithSummary("Deactivate a stage definition")
        .WithDescription("Marks the stage definition as inactive. Requires SystemAdmin role.")
        .Produces(200)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("SystemAdminPolicy")
        .RequireRateLimiting("sliding");
    }

    // ─── Stage Chains — TenantAdmin / TenantUser ────────────────────────────────

    private static void MapStageChainEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stage-chains").WithTags("StageChains");

        group.MapPost("/", async (CreateStageChainTemplateRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CreateStageChainTemplateCommand(req.TenantId, req.Name, req.IsDefault),
                ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("CreateStageChainTemplate")
        .WithSummary("Create a stage chain template")
        .WithDescription("Creates a new tenant-configurable stage chain pipeline. Requires TenantAdmin role.")
        .Accepts<CreateStageChainTemplateRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(403)
        .RequireAuthorization("TenantAdminPolicy")
        .RequireRateLimiting("sliding");

        group.MapGet("/", async (Guid tenantId, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListStageChainTemplatesQuery(tenantId), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ListStageChainTemplates")
        .WithSummary("List stage chain templates")
        .Produces<System.Collections.Generic.IReadOnlyList<StageChainTemplateDto>>(200)
        .ProducesProblem(403)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStageChainTemplateQuery(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetStageChainTemplate")
        .WithSummary("Get a stage chain template with steps")
        .Produces<StageChainTemplateDetailDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapPost("/{id:guid}/steps", async (Guid id, AddStageChainStepRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new AddStageChainStepCommand(id, req.StageDefinitionId, req.SortOrder, req.IsOptional),
                ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("AddStageChainStep")
        .WithSummary("Add a step to a stage chain template")
        .Accepts<AddStageChainStepRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("TenantAdminPolicy")
        .RequireRateLimiting("sliding");

        group.MapDelete("/{id:guid}/steps/{stageCode}", async (Guid id, string stageCode, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new RemoveStageChainStepCommand(id, stageCode), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("RemoveStageChainStep")
        .WithSummary("Remove a step from a stage chain template")
        .Produces(200)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("TenantAdminPolicy")
        .RequireRateLimiting("sliding");
    }

    // ─── Stage Handoffs — StageOperator / TenantUser ────────────────────────────

    private static void MapStageHandoffEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stage-handoffs").WithTags("StageHandoffs");

        group.MapPost("/", async (CreateStageHandoffRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateStageHandoffCommand(
                req.TenantId,
                req.FlowEpicId,
                req.SourceStageCode,
                req.TargetStageCode,
                req.IdempotencyKey,
                req.PayloadJson,
                req.SourceActorId,
                req.TargetActorId,
                req.HandshakeId), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("CreateStageHandoff")
        .WithSummary("Create a stage handoff")
        .WithDescription("Creates an immutable handoff data package for a stage transition. Requires StageOperator role.")
        .Accepts<CreateStageHandoffRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(403)
        .RequireAuthorization("StageOperatorPolicy")
        .RequireRateLimiting("sliding");

        group.MapGet("/", async (Guid flowEpicId, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStageHandoffsQuery(flowEpicId), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetStageHandoffHistory")
        .WithSummary("Get stage handoff history for a flow epic")
        .Produces<System.Collections.Generic.IReadOnlyList<StageHandoffDto>>(200)
        .ProducesProblem(403)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/latest", async (Guid flowEpicId, string src, string tgt, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLatestHandoffQuery(flowEpicId, src, tgt), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetLatestStageHandoff")
        .WithSummary("Get the latest handoff for a (FlowEpic, source, target) triple")
        .Produces<StageHandoffDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{id:guid}", (Guid id) =>
        {
            // Single handoff lookup by ID is not implemented in this scope.
            // Clients should use the history or latest endpoints.
            _ = id;
            return Results.Problem(
                detail: "Use /api/stage-handoffs?flowEpicId= for history.",
                statusCode: 404,
                type: "https://httpstatuses.io/404");
        })
        .WithName("GetStageHandoffById")
        .WithSummary("Get a single stage handoff by ID")
        .Produces<StageHandoffDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");
    }

    // ─── FlowEpic Stage Control — StageOperator ─────────────────────────────────

    private static void MapFlowEpicStageControlEndpoints(IEndpointRouteBuilder app)
    {
        var epicGroup = app.MapGroup("/api/flow-epics").WithTags("FlowEpics");

        epicGroup.MapPost("/{id:guid}/assign-chain", async (Guid id, AssignChainRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new AssignChainCommand(id, req.ChainTemplateId, req.FirstStageCode),
                ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("AssignChainToFlowEpic")
        .WithSummary("Assign a stage chain template to a flow epic")
        .Accepts<AssignChainRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("StageOperatorPolicy")
        .RequireRateLimiting("sliding");

        epicGroup.MapPost("/{id:guid}/advance-stage", async (Guid id, AdvanceStageRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new AdvanceFlowEpicStageCommand(id, req.TargetStageCode),
                ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("AdvanceFlowEpicStage")
        .WithSummary("Advance a flow epic to the next stage in its chain")
        .Accepts<AdvanceStageRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("StageOperatorPolicy")
        .RequireRateLimiting("sliding");

        epicGroup.MapPost("/{id:guid}/skip-stage", async (Guid id, SkipStageRequest req, IMediator mediator, System.Threading.CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new SkipOptionalStageCommand(id, req.StageCode),
                ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("SkipOptionalStage")
        .WithSummary("Skip an optional stage on a flow epic")
        .Accepts<SkipStageRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(403)
        .RequireAuthorization("StageOperatorPolicy")
        .RequireRateLimiting("sliding");
    }
}

// ─── Request records ─────────────────────────────────────────────────────────

/// <summary>Request body for <see cref="StageEndpoints"/> RegisterStageDefinition.</summary>
public sealed record RegisterStageDefinitionRequest(
    Guid TenantId, string StageCode, string DisplayName, string ModuleEndpoint);

/// <summary>Request body for <see cref="StageEndpoints"/> UpdateStageDefinition.</summary>
public sealed record UpdateStageDefinitionRequest(string ModuleEndpoint);

/// <summary>Request body for <see cref="StageEndpoints"/> CreateStageChainTemplate.</summary>
public sealed record CreateStageChainTemplateRequest(Guid TenantId, string Name, bool IsDefault = false);

/// <summary>Request body for <see cref="StageEndpoints"/> AddStageChainStep.</summary>
public sealed record AddStageChainStepRequest(Guid StageDefinitionId, int SortOrder, bool IsOptional = false);

/// <summary>Request body for <see cref="StageEndpoints"/> CreateStageHandoff.</summary>
public sealed record CreateStageHandoffRequest(
    Guid TenantId,
    Guid FlowEpicId,
    string SourceStageCode,
    string TargetStageCode,
    Guid IdempotencyKey,
    string PayloadJson,
    Guid? SourceActorId = null,
    Guid? TargetActorId = null,
    Guid? HandshakeId = null);

/// <summary>Request body for <see cref="StageEndpoints"/> AssignChainToFlowEpic.</summary>
public sealed record AssignChainRequest(Guid ChainTemplateId, string FirstStageCode);

/// <summary>Request body for <see cref="StageEndpoints"/> AdvanceFlowEpicStage.</summary>
public sealed record AdvanceStageRequest(string TargetStageCode);

/// <summary>Request body for <see cref="StageEndpoints"/> SkipOptionalStage.</summary>
public sealed record SkipStageRequest(string StageCode);
