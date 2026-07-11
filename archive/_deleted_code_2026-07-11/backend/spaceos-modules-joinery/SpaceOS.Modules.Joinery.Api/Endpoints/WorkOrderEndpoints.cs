using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Joinery.Application.WorkOrders.Commands.UpdateAssemblySequence;
using SpaceOS.Modules.Joinery.Application.WorkOrders.DTOs;

namespace SpaceOS.Modules.Joinery.Api.Endpoints;

/// <summary>
/// API endpoints for work order operations.
/// </summary>
public static class WorkOrderEndpoints
{
    public static void MapWorkOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/work-orders")
            .RequireAuthorization("ManufacturerOnly");

        // PATCH /api/v1/work-orders/{id}/assembly-sequence
        group.MapPatch("/{id:guid}/assembly-sequence", async (
            Guid id,
            [FromBody] UpdateAssemblySequenceRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId)
                return Results.Unauthorized();

            var cmd = new UpdateAssemblySequenceCommand(
                tenantId,
                id,
                req.Operations,
                req.Timestamp);

            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Status switch
                {
                    Ardalis.Result.ResultStatus.NotFound => Results.NotFound(new
                    {
                        error = "NOT_FOUND",
                        message = result.Errors.FirstOrDefault() ?? "Work order not found"
                    }),
                    Ardalis.Result.ResultStatus.Conflict => Results.Conflict(new
                    {
                        error = "CONCURRENT_MODIFICATION",
                        message = result.Errors.FirstOrDefault() ?? "Work order was modified by another user",
                        latest_timestamp = DateTime.UtcNow
                    }),
                    Ardalis.Result.ResultStatus.Invalid => Results.BadRequest(new
                    {
                        error = "VALIDATION_FAILED",
                        message = result.ValidationErrors.FirstOrDefault()?.ErrorMessage ?? result.Errors.FirstOrDefault() ?? "Validation failed",
                        details = result.ValidationErrors.Select(e => new
                        {
                            field = e.Identifier,
                            error = e.ErrorMessage
                        })
                    }),
                    _ => Results.Problem("An unexpected error occurred", statusCode: 500)
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("UpdateAssemblySequence")
        .WithOpenApi()
        .Produces<UpdateAssemblySequenceResponse>(200)
        .Produces(400)
        .Produces(404)
        .Produces(409)
        .Produces(422);
    }

    private static Guid? TryGetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) && id != Guid.Empty ? id : null;
    }
}
