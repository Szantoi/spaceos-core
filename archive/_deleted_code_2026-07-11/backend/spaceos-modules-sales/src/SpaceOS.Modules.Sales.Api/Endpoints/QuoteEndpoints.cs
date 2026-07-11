using MediatR;
using SpaceOS.Modules.Sales.Api.Extensions;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Quotes.Commands;
using SpaceOS.Modules.Sales.Application.Quotes.Queries;
using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the Quote resource (§6.2).
/// All endpoints require JWT authentication and per-tenant rate limiting.
/// </summary>
internal static class QuoteEndpoints
{
    internal static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/sales/api/quotes").RequireAuthorization();

        // POST /sales/api/quotes — create quote
        g.MapPost("", async (CreateQuoteCommand cmd, ISender sender) =>
        {
            var r = await sender.Send(cmd).ConfigureAwait(false);
            return r.IsSuccess
                ? Results.Created($"/sales/api/quotes/{r.Value.Id}", r.Value)
                : r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // GET /sales/api/quotes/{id}
        g.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new GetQuoteQuery(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantUser")
        .RequireRateLimiting("per-tenant");

        // GET /sales/api/quotes
        g.MapGet("", async (
            QuoteStatus? status,
            Guid? customerId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int skip,
            int take,
            ISender sender) =>
        {
            var r = await sender.Send(
                new ListQuotesQuery(status, customerId, from, to, skip, take == 0 ? 50 : take))
                .ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/quotes/{id}/lines — add line
        g.MapPost("{id:guid}/lines", async (Guid id, AddQuoteLineCommand cmd, ISender sender) =>
        {
            var bound = cmd with { QuoteId = id };
            var r = await sender.Send(bound).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // PUT /sales/api/quotes/{id}/lines/{lineId} — update line
        g.MapPut("{id:guid}/lines/{lineId:guid}", async (
            Guid id, Guid lineId, UpdateQuoteLineCommand cmd, ISender sender) =>
        {
            var bound = cmd with { QuoteId = id, LineId = lineId };
            var r = await sender.Send(bound).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // DELETE /sales/api/quotes/{id}/lines/{lineId} — remove line
        g.MapDelete("{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, ISender sender) =>
        {
            var r = await sender.Send(new RemoveQuoteLineCommand(id, lineId)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/quotes/{id}/send
        g.MapPost("{id:guid}/send", async (Guid id, SendQuoteBody body, ISender sender) =>
        {
            var r = await sender.Send(new SendQuoteCommand(id, body.ValidUntil)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/quotes/{id}/accept
        g.MapPost("{id:guid}/accept", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new AcceptQuoteCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/quotes/{id}/reject
        g.MapPost("{id:guid}/reject", async (Guid id, RejectQuoteBody body, ISender sender) =>
        {
            var r = await sender.Send(new RejectQuoteCommand(id, body.Reason)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("per-tenant");

        // POST /sales/api/quotes/{id}/convert — request conversion (202 Accepted)
        g.MapPost("{id:guid}/convert", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new RequestConversionCommand(id)).ConfigureAwait(false);
            return r.IsSuccess ? Results.Accepted() : r.ToHttpResult();
        })
        .RequireAuthorization("SalesUser")
        .RequireRateLimiting("convert");

        // DELETE /sales/api/quotes/{id} — archive
        g.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var r = await sender.Send(new ArchiveQuoteCommand(id)).ConfigureAwait(false);
            return r.ToHttpResult();
        })
        .RequireAuthorization("TenantAdmin")
        .RequireRateLimiting("per-tenant");

        return app;
    }

    private sealed record SendQuoteBody(DateTimeOffset? ValidUntil);
    private sealed record RejectQuoteBody(string Reason);
}
