using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Abstractions.Application.Calculation.Commands;
using SpaceOS.Modules.Abstractions.Application.Calculation.Queries;
using SpaceOS.Modules.Abstractions.Application.Templates.Commands;
using SpaceOS.Modules.Abstractions.Application.Templates.Queries;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Api.Endpoints;

public static class ProductTemplateEndpoints
{
    public static void MapProductTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/modules/templates")
                       .RequireAuthorization("ManufacturerOnly");

        // POST /api/modules/templates
        group.MapPost("", async (
            [FromBody] CreateTemplateRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var cmd = new CreateProductTemplateCommand(tenantId.Value, req.TradeType, req.Name);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess
                ? Results.Created($"/api/modules/templates/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.ValidationErrors);
        });

        // GET /api/modules/templates
        group.MapGet("", async (
            IMediator mediator, HttpContext ctx,
            [FromQuery] string? tradeType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(
                new ListProductTemplatesQuery(tenantId.Value, tradeType, page, pageSize), ct)
                .ConfigureAwait(false);
            return Results.Ok(result.Value);
        });

        // GET /api/modules/templates/{id}
        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(new GetProductTemplateQuery(id, tenantId.Value), ct)
                                       .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value)
                : result.Status == Ardalis.Result.ResultStatus.NotFound ? Results.NotFound()
                : Results.Forbid();
        });

        // GET /api/modules/templates/{id}/graph
        group.MapGet("{id:guid}/graph", async (Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(new GetTemplateGraphQuery(id, tenantId.Value), ct)
                                       .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value)
                : result.Status == Ardalis.Result.ResultStatus.NotFound ? Results.NotFound()
                : Results.Forbid();
        });

        // POST /api/modules/templates/{id}/slots
        group.MapPost("{id:guid}/slots", async (
            Guid id, [FromBody] AddSlotRequest req,
            IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var cmd = new AddComponentSlotCommand(id, tenantId.Value, req.Name, req.ComponentType,
                req.DefaultMaterial, req.DefaultThickness, req.Quantity, req.IsVirtual,
                req.SemanticRole, req.SortOrder);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });

        // POST /api/modules/templates/{id}/connections
        group.MapPost("{id:guid}/connections", async (
            Guid id, [FromBody] AddConnectionRequest req,
            IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var cmd = new AddSlotConnectionCommand(id, tenantId.Value, req.ParentSlotId, req.ChildSlotId,
                req.Axis, req.Operator, req.Operand, req.MultiplierCount, req.SecondaryParentSlotId,
                req.JointType, req.MachiningOp, req.ProcessPhase);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // PUT /api/modules/templates/{id}/parameters/{key}
        group.MapPut("{id:guid}/parameters/{key}", async (
            Guid id, string key, [FromBody] SetParameterRequest req,
            IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var cmd = new SetTemplateParameterCommand(id, tenantId.Value, key, req.Value, req.Description);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        });

        // POST /api/modules/templates/{id}/clone
        group.MapPost("{id:guid}/clone", async (
            Guid id, IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(new CloneProductTemplateCommand(id, tenantId.Value), ct)
                                       .ConfigureAwait(false);
            return result.IsSuccess
                ? Results.Created($"/api/modules/templates/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        });

        // POST /api/modules/templates/{id}/calculate
        group.MapPost("{id:guid}/calculate", async (
            Guid id, [FromBody] CalculateRequest req,
            IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var cmd = new CalculateProductCommand(id, tenantId.Value, req.Width, req.Height, req.Depth,
                req.ParameterOverrides);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // GET /api/modules/templates/{id}/cutting-list
        group.MapGet("{id:guid}/cutting-list", async (
            Guid id, IMediator mediator, HttpContext ctx, HttpResponse response,
            [FromQuery] decimal w, [FromQuery] decimal h, [FromQuery] decimal d,
            CancellationToken ct) =>
        {
            response.Headers["Cache-Control"] = "no-store"; // SEC-05
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(new GetCuttingListQuery(id, tenantId.Value, w, h, d), ct)
                                       .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // GET /api/modules/templates/{id}/cnc-plan?w=900&h=2100&d=40
        group.MapGet("{id:guid}/cnc-plan", async (
            Guid id, IMediator mediator, HttpContext ctx, HttpResponse response,
            [FromQuery] decimal w, [FromQuery] decimal h, [FromQuery] decimal d,
            CancellationToken ct) =>
        {
            response.Headers["Cache-Control"] = "no-store";
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(new GetCncPlanQuery(id, tenantId.Value, w, h, d), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // GET /api/modules/templates/{id}/process-plan?w=900&h=2100&d=40
        group.MapGet("{id:guid}/process-plan", async (
            Guid id, IMediator mediator, HttpContext ctx, HttpResponse response,
            [FromQuery] decimal w, [FromQuery] decimal h, [FromQuery] decimal d,
            CancellationToken ct) =>
        {
            response.Headers["Cache-Control"] = "no-store";
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var result = await mediator.Send(new GetProcessPlanQuery(id, tenantId.Value, w, h, d), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // POST /api/templates/{name}/calculate  (name-based shortcut)
        var nameGroup = app.MapGroup("/api/templates")
                           .RequireAuthorization("ManufacturerOnly");

        nameGroup.MapPost("{name}/calculate", async (
            string name, [FromBody] CalculateRequest req,
            IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantId = GetTenantId(ctx);
            if (tenantId == null) return Results.Forbid();
            var cmd = new CalculateByNameCommand(name, tenantId.Value, req.Width, req.Height, req.Depth,
                req.ParameterOverrides);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value)
                : result.Status == Ardalis.Result.ResultStatus.NotFound ? Results.NotFound()
                : result.Status == Ardalis.Result.ResultStatus.Forbidden ? Results.Forbid()
                : Results.BadRequest(result.Errors);
        });
    }

    private static Guid? GetTenantId(HttpContext ctx)
    {
        var claim = ctx.User?.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

// Request DTOs
public sealed record CreateTemplateRequest(string TradeType, string Name);
public sealed record AddSlotRequest(
    string Name, string ComponentType, string? DefaultMaterial, decimal? DefaultThickness,
    int Quantity, bool IsVirtual, SemanticRole? SemanticRole, int SortOrder);
public sealed record AddConnectionRequest(
    Guid ParentSlotId, Guid ChildSlotId,
    DimensionAxis Axis, RuleOperator Operator, decimal Operand,
    int? MultiplierCount, Guid? SecondaryParentSlotId,
    JointType JointType, MachiningOperation MachiningOp, ProcessPhase ProcessPhase);
public sealed record SetParameterRequest(decimal Value, string? Description);
public sealed record CalculateRequest(
    decimal Width, decimal Height, decimal Depth,
    IReadOnlyDictionary<string, decimal>? ParameterOverrides = null);
