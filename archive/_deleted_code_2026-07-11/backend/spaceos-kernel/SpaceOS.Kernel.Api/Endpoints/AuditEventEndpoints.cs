// SpaceOS.Kernel.Api/Endpoints/AuditEventEndpoints.cs

using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.AuditLog.Commands;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers AuditLog Minimal API endpoints.</summary>
public static class AuditEventEndpoints
{
    /// <summary>Maps all audit event endpoints to the provided route builder.</summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapAuditEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit-events").WithTags("AuditLog");

        group.MapGet("/", async (
            Guid? tenantId,
            string? eventType,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page = 1,
            int pageSize = 20,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            // Normalize date-only `to` values: "2026-04-02" parses to midnight, which would
            // exclude events from that day. Shift to end-of-day so the upper bound is inclusive.
            var toNormalized = to.HasValue && to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.AddDays(1).AddTicks(-1)
                : to;

            var result = await mediator.Send(
                new GetAuditEventsQuery(tenantId, eventType, from, toNormalized, page, pageSize), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetAuditEvents")
        .WithSummary("List audit events (paged)")
        .WithDescription("Returns a paged list of audit events, optionally filtered by tenant and/or date range. Requires authentication.")
        .Produces<PagedList<AuditEventDto>>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("fixed");

        group.MapGet("/verify-chain", async (
            Guid? tenantId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            if (!tenantId.HasValue)
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["tenantId"] = ["The tenantId query parameter is required."]
                    });

            var result = await mediator.Send(
                new VerifyChainQuery(tenantId.Value, from, to), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("VerifyAuditChain")
        .WithSummary("Verify audit event chain integrity")
        .WithDescription("Walks the audit event chain for the specified tenant and date range, verifying that every PreviousHash link is intact and that state hashes match the external sink.")
        .Produces<ChainVerificationResultDto>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("fixed");

        group.MapPost("/re-hash", async (
            ReHashRequest request,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            if (!Enum.TryParse<HashAlgorithmType>(request.TargetAlgorithm, ignoreCase: true, out var algorithm))
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["targetAlgorithm"] = [$"'{request.TargetAlgorithm}' is not a valid HashAlgorithmType. Valid values: {string.Join(", ", Enum.GetNames<HashAlgorithmType>())}."]
                    });

            var result = await mediator
                .Send(new ReHashChainCommand(request.TenantId, algorithm), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("ReHashAuditChain")
        .WithSummary("Dry-run: count audit records that would be affected by an algorithm migration")
        .WithDescription(
            "Performs a read-only dry-run analysis of re-hashing a tenant's audit event chain " +
            "with a new cryptographic algorithm. Returns how many records would need to be re-hashed. " +
            "No data is modified. Admin-only.")
        .Produces<ReHashResultDto>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}

/// <summary>Request body for the <c>POST /api/audit-events/re-hash</c> endpoint.</summary>
/// <param name="TenantId">The tenant whose audit chain to analyse.</param>
/// <param name="TargetAlgorithm">The target algorithm name (e.g. "SHA3_256").</param>
public sealed record ReHashRequest(Guid TenantId, string TargetAlgorithm);
