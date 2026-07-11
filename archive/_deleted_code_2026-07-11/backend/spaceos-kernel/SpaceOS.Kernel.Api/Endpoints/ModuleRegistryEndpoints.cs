// SpaceOS.Kernel.Api/Endpoints/ModuleRegistryEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.ModuleRegistry;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Module Registry read endpoints.</summary>
public static class ModuleRegistryEndpoints
{
    /// <summary>Maps the <c>GET /api/module-registry/{tenantType}</c> endpoint.</summary>
    public static IEndpointRouteBuilder MapModuleRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/module-registry").WithTags("ModuleRegistry");

        group.MapGet("/{tenantType}", async (
            string tenantType,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new GetModuleRegistryQuery(tenantType), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetModuleRegistry")
        .WithSummary("Get module registry for a tenant type")
        .WithDescription("Returns the required and allowed modules for the given TenantType. Case-insensitive.")
        .Produces<ModuleRegistryDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}
