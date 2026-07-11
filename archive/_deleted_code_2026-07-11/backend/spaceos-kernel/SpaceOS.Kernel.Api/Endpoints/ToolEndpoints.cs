// SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs
using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Tools.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the LLM Tool Registry.
/// All endpoints require authentication and source TenantId from the JWT claim.
/// </summary>
public static class ToolEndpoints
{
    /// <summary>Maps tool registry query endpoints to the application.</summary>
    public static IEndpointRouteBuilder MapToolEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tools")
            .RequireAuthorization()
            .WithTags("Tools");

        // GET /api/tools/flow-epics?page=1&pageSize=20
        group.MapGet("/flow-epics", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(user);
            if (tenantId == Guid.Empty)
                return Results.Problem(
                    title:      "Unauthorized",
                    detail:     "A valid JWT Bearer token with a tenant claim is required.",
                    statusCode: 401,
                    type:       "https://tools.ietf.org/html/rfc7235#section-3.1");

            var result = await sender
                .Send(new ListFlowEpicsQuery(tenantId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ListFlowEpics")
        .WithSummary("List FlowEpics for the authenticated tenant (paginated).")
        .Produces<PagedList<FlowEpicSummaryDto>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/tools/workstations?page=1&pageSize=20
        group.MapGet("/workstations", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(user);
            if (tenantId == Guid.Empty)
                return Results.Problem(
                    title:      "Unauthorized",
                    detail:     "A valid JWT Bearer token with a tenant claim is required.",
                    statusCode: 401,
                    type:       "https://tools.ietf.org/html/rfc7235#section-3.1");

            var result = await sender
                .Send(new ListWorkStationsQuery(tenantId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ListWorkStations")
        .WithSummary("List WorkStations for the authenticated tenant (paginated).")
        .Produces<PagedList<WorkStationSummaryDto>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/tools/facilities?page=1&pageSize=20
        group.MapGet("/facilities", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(user);
            if (tenantId == Guid.Empty)
                return Results.Problem(
                    title:      "Unauthorized",
                    detail:     "A valid JWT Bearer token with a tenant claim is required.",
                    statusCode: 401,
                    type:       "https://tools.ietf.org/html/rfc7235#section-3.1");

            var result = await sender
                .Send(new ListFacilitiesQuery(tenantId, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ListFacilities")
        .WithSummary("List Facilities for the authenticated tenant (paginated).")
        .Produces<PagedList<FacilitySummaryDto>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/tools/summary
        group.MapGet("/summary", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(user);
            if (tenantId == Guid.Empty)
                return Results.Problem(
                    title:      "Unauthorized",
                    detail:     "A valid JWT Bearer token with a tenant claim is required.",
                    statusCode: 401,
                    type:       "https://tools.ietf.org/html/rfc7235#section-3.1");

            var result = await sender
                .Send(new GetTenantSummaryQuery(tenantId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetTenantSummary")
        .WithSummary("Get aggregate counts for the authenticated tenant.")
        .Produces<TenantSummaryDto>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    // JWT claim type URI for the Microsoft/Azure AD tenant-id inbound claim mapping.
    // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap maps "tid" → this URI,
    // so FindFirst("tid") returns null; we must also check the mapped name.
    private const string MicrosoftTenantIdClaimType =
        "http://schemas.microsoft.com/identity/claims/tenantid";

    /// <summary>
    /// Resolves the current tenant GUID from JWT claims.
    /// Priority: (1) Keycloak <c>spaceos_tenants</c> claims — ASP.NET JsonWebTokenHandler splits
    /// JSON array elements into individual <see cref="Claim"/> objects with the same name,
    /// so each claim value is a single JSON object like <c>{"tenant_id":"..."}</c>;
    /// (2) legacy flat <c>tid</c> claim — note: when <c>MapInboundClaims = true</c> (default)
    /// the JWT handler remaps <c>"tid"</c> → <see cref="MicrosoftTenantIdClaimType"/> URI, so
    /// both names are checked; (3) <c>tenant_id</c> flat claim.
    /// Returns <see cref="Guid.Empty"/> when no tenant claim is present.
    /// </summary>
    private static Guid GetTenantId(ClaimsPrincipal user)
    {
        // (1) Keycloak spaceos_tenants — ASP.NET splits array → individual Claims (KC-T2).
        // Each claim value may be:
        //   a) A JSON object string: {"tenant_id":"<guid>",...}      → direct TryGetProperty
        //   b) A JSON array string:  [{"tenant_id":"<guid>",...},...]→ double-serialized by
        //      Keycloak Script Mapper; iterate array elements.
        foreach (var claim in user.FindAll("spaceos_tenants"))
        {
            try
            {
                using var doc = JsonDocument.Parse(claim.Value);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("tenant_id", out var arrEl) &&
                            Guid.TryParse(arrEl.GetString(), out var ag) &&
                            ag != Guid.Empty)
                            return ag;
                    }
                }
                else if (doc.RootElement.TryGetProperty("tenant_id", out var idEl) &&
                         Guid.TryParse(idEl.GetString(), out var g) &&
                         g != Guid.Empty)
                {
                    return g;
                }
            }
            catch { /* malformed claim value — try next */ }
        }

        // (2) Legacy flat tid claim — MapInboundClaims remaps "tid" → tenantid URI, check both
        var tidStr = user.FindFirst(MicrosoftTenantIdClaimType)?.Value
                  ?? user.FindFirst("tid")?.Value
                  ?? user.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tidStr, out var tid) ? tid : Guid.Empty;
    }
}
