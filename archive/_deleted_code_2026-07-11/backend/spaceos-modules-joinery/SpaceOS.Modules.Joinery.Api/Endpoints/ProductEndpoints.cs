using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Modules.Joinery.Application.Products.Commands.ConfigureProduct;
using SpaceOS.Modules.Joinery.Application.Products.Commands.CreateWorkOrder;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Application.Products.Services;

namespace SpaceOS.Modules.Joinery.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .RequireAuthorization("ManufacturerOnly");

        // POST /api/products/configure
        group.MapPost("/configure", async (
            [FromBody] ConfigureProductRequest req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

            var userId = TryGetUserId(ctx);
            var cmd = new ConfigureProductCommand(
                tenantId,
                req.ProductType,
                req.Dimensions,
                req.Materials,
                req.Fittings,
                userId);

            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Status == Ardalis.Result.ResultStatus.NotFound
                    ? Results.NotFound(result.Errors)
                    : Results.BadRequest(result.ValidationErrors.Select(e => new { e.Identifier, e.ErrorMessage }));
            }

            return Results.Ok(result.Value);
        })
        .WithName("ConfigureProduct")
        .WithOpenApi();

        // POST /api/work-orders
        app.MapGroup("/api/work-orders")
            .RequireAuthorization("ManufacturerOnly")
            .MapPost("/", async (
                [FromBody] CreateWorkOrderRequest req,
                IMediator mediator,
                HttpContext ctx,
                CancellationToken ct) =>
            {
                if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

                // Parse config ID (GUID format)
                if (!Guid.TryParse(req.ConfigId, out var configurationId) || configurationId == Guid.Empty)
                    return Results.BadRequest(new { Error = "Invalid ConfigId format. Expected: valid GUID" });

                var userId = TryGetUserId(ctx);
                var cmd = new CreateWorkOrderCommand(
                    tenantId,
                    configurationId,
                    req.Quantity,
                    req.DeliveryDate,
                    req.CustomerRef,
                    req.Notes,
                    userId);

                var result = await mediator.Send(cmd, ct).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    return result.Status == Ardalis.Result.ResultStatus.NotFound
                        ? Results.NotFound(result.Errors)
                        : Results.BadRequest(result.ValidationErrors.Select(e => new { e.Identifier, e.ErrorMessage }));
                }

                return Results.Created($"/api/work-orders/{result.Value.WorkOrderId}", result.Value);
            })
            .WithName("CreateWorkOrder")
            .WithOpenApi();

        // GET /api/work-orders/{id}/sheet.pdf
        app.MapGroup("/api/work-orders")
            .RequireAuthorization("ManufacturerOnly")
            .MapGet("/{id:guid}/sheet.pdf", async (
                Guid id,
                IWorkOrderPdfService pdfService,
                HttpContext ctx,
                CancellationToken ct) =>
            {
                if (TryGetTenantId(ctx) is not { } tenantId) return Results.Unauthorized();

                var pdfStream = await pdfService.GetWorkOrderPdfStreamAsync(id, tenantId, ct).ConfigureAwait(false);
                if (pdfStream is null)
                    return Results.NotFound();

                ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
                ctx.Response.Headers["Cache-Control"] = "private, no-store";

                return Results.File(pdfStream, "application/pdf",
                    fileDownloadName: $"work_order_{id:N}.pdf",
                    enableRangeProcessing: false);
            })
            .WithName("GetWorkOrderPdf")
            .Produces(200, contentType: "application/pdf");
    }

    private static Guid? TryGetTenantId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) && id != Guid.Empty ? id : null;
    }

    private static Guid? TryGetUserId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

}
