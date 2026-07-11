using MediatR;
using SpaceOS.Modules.Sales.Api.Extensions;
using SpaceOS.Modules.Sales.Application.Pipeline.Queries;

namespace SpaceOS.Modules.Sales.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for sales pipeline analytics (§6.3).
/// </summary>
internal static class PipelineEndpoints
{
    internal static IEndpointRouteBuilder MapPipelineEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/sales/api/pipeline").RequireAuthorization();

        // GET /sales/api/pipeline/funnel
        g.MapGet("funnel", async (DateTimeOffset? from, DateTimeOffset? to, ISender sender) =>
        {
            var r = await sender.Send(new GetSalesFunnelQuery(from, to)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantUser")
        .RequireRateLimiting("per-tenant");

        // GET /sales/api/pipeline/conversion-rate
        g.MapGet("conversion-rate", async (DateTimeOffset? from, DateTimeOffset? to, ISender sender) =>
        {
            var r = await sender.Send(new GetConversionRateQuery(from, to)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantUser")
        .RequireRateLimiting("per-tenant");

        return app;
    }
}
