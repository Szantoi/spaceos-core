using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Commands.GenerateAnyaglista;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Queries.GetAnyaglista;

public static class AnyaglistaEndpoints
{
    public static void MapAnyaglistaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/anyaglista")
            .RequireAuthorization("ManufacturerOnly");

        // POST /api/anyaglista/generate
        group.MapPost("/generate", async (
            [FromBody] GenerateAnyaglistaRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var cmd = new GenerateAnyaglistaCommand(tenantId, req.OrderId);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Created(
                    $"/api/anyaglista/{req.OrderId}",
                    result.Value),
                ResultStatus.NotFound => Results.NotFound(result.Errors),
                ResultStatus.Error => Results.Problem(
                    detail: string.Join(", ", result.Errors),
                    statusCode: 500),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("GenerateAnyaglista")
        .Produces<GenerateAnyaglistaResponse>(201)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // GET /api/anyaglista/{orderId}
        group.MapGet("/{orderId:guid}", async (
            Guid orderId,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var result = await mediator.Send(
                new GetAnyaglistaQuery(tenantId, orderId), ct).ConfigureAwait(false);

            return result.Status switch
            {
                ResultStatus.Ok => Results.Ok(result.Value),
                ResultStatus.NotFound => Results.NotFound(),
                _ => Results.BadRequest(result.Errors)
            };
        })
        .WithName("GetAnyaglista")
        .Produces<GetAnyaglistaResponse>();
    }

    // SEC-01: Guid.Empty or missing tenant_id claim → null → 401
    private static Guid? TryGetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) && id != Guid.Empty ? id : null;
    }
}
