// SpaceOS.Kernel.Api/Endpoints/TenantEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Facilities.Commands;
using SpaceOS.Kernel.Application.Facilities.Queries;
using SpaceOS.Kernel.Application.Tenants;
using SpaceOS.Kernel.Application.Tenants.Commands;
using SpaceOS.Kernel.Application.Tenants.Queries;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Tenant-related Minimal API endpoints (reads and writes).</summary>
public static class TenantEndpoints
{
    /// <summary>Maps all Tenant GET and write endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants");

        // --- GET ---

        group.MapGet("/", async (
            int page = 1,
            int pageSize = 20,
            string? tenantType = null,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            TenantType? typeFilter = null;
            if (tenantType is not null)
            {
                if (!Enum.TryParse<TenantType>(tenantType, ignoreCase: true, out var parsed))
                    return Results.Problem(
                        title: "Invalid tenantType",
                        detail: $"Unknown TenantType: '{tenantType}'. Valid values: {string.Join(", ", Enum.GetNames<TenantType>())}",
                        statusCode: 400);
                typeFilter = parsed;
            }

            var result = await mediator.Send(new GetAllTenantsQuery(page, pageSize, typeFilter), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetAllTenants")
        .WithSummary("List all tenants (paged)")
        .WithDescription("Returns a paged list of all tenants. Requires the ReadPolicy role. Supports `page`, `pageSize`, and optional `tenantType` filter query parameters.")
        .Produces<PagedList<TenantDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(400)
        .ProducesProblem(429)
        .ProducesProblem(404)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTenantByIdQuery(id), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetTenantById")
        .WithSummary("Get a tenant by ID")
        .WithDescription("Returns a single tenant by its unique identifier. Returns 404 if not found.")
        .Produces<TenantDto>(200)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        group.MapGet("/{tenantId:guid}/facilities", async (
            Guid tenantId,
            int page = 1,
            int pageSize = 20,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetFacilitiesByTenantQuery(tenantId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetFacilitiesByTenant")
        .WithSummary("List facilities for a tenant (paged)")
        .WithDescription("Returns a paged list of facilities belonging to the specified tenant. Returns 404 if the tenant does not exist.")
        .Produces<PagedList<FacilityDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("ReadPolicy")
        .RequireRateLimiting("fixed");

        // --- POST ---

        group.MapPost("/", async (CreateTenantRequest request, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<TenantType>(request.TenantType, ignoreCase: true, out var tenantType))
                return Results.Problem(
                    title: "Invalid TenantType",
                    detail: $"Unknown TenantType: '{request.TenantType}'. Valid values: {string.Join(", ", Enum.GetNames<TenantType>())}",
                    statusCode: 400);

            var result = await mediator
                .Send(new CreateTenantCommand(request.Name, tenantType, request.EnabledModules), ct)
                .ConfigureAwait(false);
            return result.ToCreatedResult("GetTenantById", id => new { id });
        })
        .WithName("CreateTenant")
        .WithSummary("Create a new tenant")
        .WithDescription("Creates a new tenant with the given name. Requires AdminPolicy. Returns the new tenant ID on success.")
        .Accepts<CreateTenantRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .ProducesProblem(500)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("sliding");

        group.MapPost("/{tenantId:guid}/facilities", async (
            Guid tenantId, CreateFacilityRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new CreateFacilityCommand(tenantId, request.Name), ct)
                .ConfigureAwait(false);
            return result.ToCreatedResult("GetFacilityById", id => new { id });
        })
        .WithName("CreateFacility")
        .WithSummary("Create a facility under a tenant")
        .WithDescription("Creates a new facility under the specified tenant. Requires WritePolicy. Returns the new facility ID on success.")
        .Accepts<CreateFacilityRequest>("application/json")
        .Produces<Guid>(201)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .ProducesProblem(500)
        .RequireAuthorization("WritePolicy")
        .RequireRateLimiting("sliding");

        // --- PUT ---

        group.MapPut("/{id:guid}/modules", async (Guid id, UpdateTenantModulesRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new UpdateTenantModulesCommand(id, request.Modules ?? Array.Empty<string>()), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UpdateTenantModules")
        .WithSummary("Update a tenant's enabled modules")
        .WithDescription("Replaces the enabled module list for an existing tenant. Modules are validated against the tenant's TenantType. Requires AdminPolicy.")
        .Accepts<UpdateTenantModulesRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("sliding");

        group.MapPut("/{id:guid}", async (Guid id, UpdateTenantNameRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new UpdateTenantNameCommand(id, request.Name), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("UpdateTenantName")
        .WithSummary("Update a tenant's display name")
        .WithDescription("Renames an existing tenant. Requires AdminPolicy. Returns 404 if the tenant does not exist.")
        .Accepts<UpdateTenantNameRequest>("application/json")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("sliding");

        // --- DELETE ---

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ArchiveTenantCommand(id), ct).ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ArchiveTenant")
        .WithSummary("Archive a tenant")
        .WithDescription("Soft-deletes a tenant by setting IsArchived = true. Returns 204 on success, 404 if not found, 409 if already archived.")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("sliding");

        return app;
    }
}

/// <summary>Request body for creating a new Tenant.</summary>
/// <param name="Name">The display name for the tenant.</param>
/// <param name="TenantType">The ecosystem actor type. Defaults to "Manufacturer".</param>
/// <param name="EnabledModules">Optional initial set of enabled module names.</param>
public sealed record CreateTenantRequest(string Name, string TenantType = "Manufacturer", string[]? EnabledModules = null);

/// <summary>Request body for updating a Tenant's display name.</summary>
/// <param name="Name">The new display name.</param>
public sealed record UpdateTenantNameRequest(string Name);

/// <summary>Request body for creating a new Facility under a Tenant.</summary>
/// <param name="Name">The display name for the new facility.</param>
public sealed record CreateFacilityRequest(string Name);

/// <summary>Request body for updating a Tenant's enabled modules.</summary>
/// <param name="Modules">The new set of enabled module names (e.g. ["door", "cutting"]).</param>
public sealed record UpdateTenantModulesRequest(string[]? Modules);
