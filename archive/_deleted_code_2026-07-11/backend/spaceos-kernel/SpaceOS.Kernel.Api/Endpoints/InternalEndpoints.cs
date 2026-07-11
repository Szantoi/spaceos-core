// SpaceOS.Kernel.Api/Endpoints/InternalEndpoints.cs
using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Internal.Dtos;
using SpaceOS.Kernel.Application.Internal.Queries;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>
/// Registers internal test-infrastructure endpoints.
/// All endpoints require the <c>X-SpaceOS-Internal</c> header to be present
/// and are excluded from public Swagger documentation.
/// </summary>
public static class InternalEndpoints
{
    /// <summary>Maps internal test-infrastructure endpoints to the provided route builder.</summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal")
            .WithTags("Internal")
            .ExcludeFromDescription()
            .AllowAnonymous();

        // BE-TEST-02: Delete all FlowEpic + Snapshot + AuditEvent data for a given test tenant.
        // Defense-in-depth: X-SpaceOS-Internal header + confirm=true + TEST_TENANT_ALLOWLIST env var.
        group.MapDelete("/flow-epics/by-tenant/{tenantId}", async (
            string tenantId,
            bool? confirm,
            HttpContext httpContext,
            IFlowEpicRepository repo,
            IConfiguration config) =>
        {
            // Gate 1: X-SpaceOS-Internal header
            if (!httpContext.Request.Headers.TryGetValue("X-SpaceOS-Internal", out var internalHeader)
                || !string.Equals(internalHeader, "true", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Json(
                    new { error = "Forbidden", message = "Missing or invalid X-SpaceOS-Internal header" },
                    statusCode: 403);
            }

            // Gate 2: confirm=true safety parameter
            if (confirm != true)
            {
                return Results.Json(
                    new { error = "Bad request", message = "Missing confirm=true parameter" },
                    statusCode: 400);
            }

            // Gate 3: GUID format validation
            if (!Guid.TryParse(tenantId, out var tenantGuid))
            {
                return Results.Json(
                    new { error = "Bad request", message = "Invalid tenantId format" },
                    statusCode: 400);
            }

            // Gate 4: TEST_TENANT_ALLOWLIST — defense in depth
            var allowlist = (config["TEST_TENANT_ALLOWLIST"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowlist.Contains(tenantId, StringComparer.OrdinalIgnoreCase))
            {
                return Results.Json(
                    new { error = "Forbidden", message = "Tenant not in test allowlist" },
                    statusCode: 403);
            }

            // Execute deletion
            var counts = await repo.DeleteAllByTenantAsync(tenantGuid).ConfigureAwait(false);

            return Results.Ok(new
            {
                tenantId,
                deletedCounts = new
                {
                    flowEpics = counts.FlowEpics,
                    snapshots = counts.Snapshots,
                    auditEvents = counts.AuditEvents,
                }
            });
        })
        .WithName("DeleteFlowEpicsByTenant")
        .DisableRateLimiting();

        // PartnerTier: AttributionWorker email-hash lookup.
        // Returns tenantId for a given SHA-256(email) hex string.
        group.MapGet("/tenants/by-email-hash", async (
            string? hash,
            HttpContext httpContext,
            ITenantRepository tenantRepository,
            CancellationToken ct) =>
        {
            // Gate: X-SpaceOS-Internal header
            if (!httpContext.Request.Headers.TryGetValue("X-SpaceOS-Internal", out var internalHeader)
                || !string.Equals(internalHeader, "true", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Json(
                    new { error = "Forbidden", message = "Missing or invalid X-SpaceOS-Internal header" },
                    statusCode: 403);
            }

            if (string.IsNullOrWhiteSpace(hash))
            {
                return Results.Json(
                    new { error = "Bad request", message = "hash query parameter is required" },
                    statusCode: 400);
            }

            var tenant = await tenantRepository.GetByEmailHashAsync(hash, ct).ConfigureAwait(false);

            if (tenant is null)
                return Results.NotFound(new { error = "Not found", message = "No tenant found for the given email hash" });

            return Results.Ok(new { tenantId = tenant.Id.Value });
        })
        .WithName("GetTenantByEmailHash")
        .DisableRateLimiting();

        // ADR-039: Actor Directory — Sales Customer→Platform Actor link validation
        var apiInternalGroup = app.MapGroup("/api/internal")
            .WithTags("Internal");

        apiInternalGroup.MapGet("/tenants/{id:guid}", async (
            Guid id,
            HttpContext httpCtx,
            ISender mediator,
            CancellationToken ct) =>
        {
            var requesterHeader = httpCtx.Request.Headers["X-SpaceOS-TenantId"].FirstOrDefault();

            if (!Guid.TryParse(requesterHeader, out var requesterTenantId))
            {
                return Results.Json(
                    new { error = "X-SpaceOS-TenantId header is missing or not a valid GUID." },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await mediator
                .Send(new GetTenantActorQuery(requesterTenantId, id), ct)
                .ConfigureAwait(false);

            return result.ToApiResult();
        })
        .WithName("GetTenantActorInternal")
        .Produces<TenantActorResponse>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .ExcludeFromDescription()
        .DisableRateLimiting();

        return app;
    }
}
