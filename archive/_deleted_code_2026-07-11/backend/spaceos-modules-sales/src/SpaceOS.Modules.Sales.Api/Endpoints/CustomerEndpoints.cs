using MediatR;
using SpaceOS.Modules.Sales.Api.Extensions;
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Application.Customers.Queries;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the Customer resource (§6.1).
/// All endpoints require JWT authentication and per-tenant rate limiting.
/// </summary>
internal static class CustomerEndpoints
{
    internal static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/sales/api/customers").RequireAuthorization();

        // POST /sales/api/customers — create customer
        g.MapPost("", async (CreateCustomerCommand cmd, ISender sender) =>
        {
            var r = await sender.Send(cmd).ConfigureAwait(false);
            return r.IsSuccess
                ? Results.Created($"/sales/api/customers/{r.Value.Id}", r.Value)
                : r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // GET /sales/api/customers/{id}
        g.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new GetCustomerQuery(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantUser")
        .RequireRateLimiting("per-tenant");

        // GET /sales/api/customers
        g.MapGet("", async (
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            ISender sender) =>
        {
            var r = await sender.Send(
                new ListCustomersQuery(status, search, skip, take == 0 ? 50 : take))
                .ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantUser")
        .RequireRateLimiting("per-tenant");

        // PUT /sales/api/customers/{id}/contact
        g.MapPut("{id:guid}/contact", async (Guid id, UpdateCustomerContactCommand cmd, ISender sender) =>
        {
            var bound = cmd with { CustomerId = id };
            var r = await sender.Send(bound).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // PUT /sales/api/customers/{id}/addresses
        g.MapPut("{id:guid}/addresses", async (Guid id, UpdateCustomerAddressesCommand cmd, ISender sender) =>
        {
            var bound = cmd with { CustomerId = id };
            var r = await sender.Send(bound).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/customers/{id}/promote
        g.MapPost("{id:guid}/promote", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new PromoteCustomerCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/customers/{id}/deactivate
        g.MapPost("{id:guid}/deactivate", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new DeactivateCustomerCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // DELETE /sales/api/customers/{id}
        g.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new ArchiveCustomerCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantAdmin")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/customers/{id}/link
        g.MapPost("{id:guid}/link", async (Guid id, LinkCustomerToActorBody body, ISender sender) =>
        {
            var r = await sender.Send(
                new LinkCustomerToActorCommand(id, body.PlatformTenantId))
                .ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantAdmin")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/customers/{id}/link/refresh
        g.MapPost("{id:guid}/link/refresh", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new RefreshCustomerLinkCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantAdmin")
        .RequireRateLimiting("per-tenant");

        // DELETE /sales/api/customers/{id}/link
        g.MapDelete("{id:guid}/link", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new UnlinkCustomerFromActorCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantAdmin")
        .RequireRateLimiting("per-tenant");

        return app;
    }

    private sealed record LinkCustomerToActorBody(Guid PlatformTenantId);
}
