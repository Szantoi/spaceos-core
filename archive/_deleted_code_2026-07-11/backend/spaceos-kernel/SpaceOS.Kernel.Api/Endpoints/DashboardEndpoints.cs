// SpaceOS.Kernel.Api/Endpoints/DashboardEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Dashboard;
using SpaceOS.Kernel.Application.Dashboard.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Dashboard-related Minimal API endpoints.</summary>
public static class DashboardEndpoints
{
    /// <summary>Maps the dashboard stats GET endpoint to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/stats", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDashboardStatsQuery(), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetDashboardStats")
        .WithSummary("Get system-wide dashboard statistics")
        .WithDescription("Returns aggregated counts for tenants, facilities, workstations, flow epics, and audit events. All counts are fetched in a single database round-trip. Requires ReadPolicy.")
        .Produces<DashboardStatsDto>(200)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}
