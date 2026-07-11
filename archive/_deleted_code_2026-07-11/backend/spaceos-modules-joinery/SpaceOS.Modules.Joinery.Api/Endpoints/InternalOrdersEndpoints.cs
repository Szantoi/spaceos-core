using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Api.Internal.Dtos;

internal static class InternalOrdersEndpoints
{
    internal static IEndpointRouteBuilder MapInternalOrdersEndpoints(
        this IEndpointRouteBuilder app)
    {
        var internalGroup = app
            .MapGroup("/joinery/internal")
            .ExcludeFromDescription();

        internalGroup.MapPost("/orders/from-quote",
            async (
                HttpContext ctx,
                OrderConversionRequestDto body,
                IMediator mediator,
                CancellationToken ct) =>
            {
                // SEC-S-01: strict-equal header vs body TenantId
                var headerTenantId = (Guid)ctx.Items["InternalTenantId"]!;
                if (headerTenantId != body.TenantId)
                    return Results.Problem(
                        statusCode: 400,
                        title: "TenantId mismatch",
                        detail: "X-SpaceOS-TenantId header must equal request body TenantId.");

                var result = await mediator.Send(body.ToCommand(), ct).ConfigureAwait(false);

                return result.Status switch
                {
                    ResultStatus.Ok =>
                        Results.Ok(new { orderId = result.Value.OrderId, createdAt = result.Value.CreatedAt }),
                    ResultStatus.Conflict =>
                        Results.Conflict(new { error = result.Errors.FirstOrDefault() }),
                    ResultStatus.Invalid =>
                        Results.BadRequest(new { errors = result.ValidationErrors.Select(e => e.ErrorMessage) }),
                    _ =>
                        Results.Problem(statusCode: 500, title: "Internal error")
                };
            })
            .AllowAnonymous()
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .Produces(409);

        return app;
    }
}
