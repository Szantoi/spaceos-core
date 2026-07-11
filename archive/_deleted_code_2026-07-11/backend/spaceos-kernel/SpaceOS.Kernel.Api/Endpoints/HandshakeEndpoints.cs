// SpaceOS.Kernel.Api/Endpoints/HandshakeEndpoints.cs

using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Handshakes.Queries;
using SpaceOS.Kernel.Domain.DTOs;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers B2B handshake allowlist Minimal API endpoints.</summary>
public static class HandshakeEndpoints
{
    /// <summary>Maps all handshake-related endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapHandshakeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/handshakes").WithTags("Handshakes");

        group.MapGet("/allowed-hosts", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllowedHostsQuery(), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetAllowedHosts")
        .WithSummary("Get the list of allowed B2B host tenants for the current tenant")
        .WithDescription("Returns up to 20 host tenants that the authenticated tenant is permitted to initiate B2B handshakes with (SEC-P3CP-08). The response is derived from the TenantHandshakeAllowlist table.")
        .Produces<IReadOnlyList<AllowedHostDto>>(200)
        .ProducesProblem(401)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}
