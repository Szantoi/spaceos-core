using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Internal.Dtos;
using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Internal.Queries;

/// <summary>
/// Handles GET /api/internal/tenants/{id} — ADR-039 read path.
/// SEC-S-09: returns minimal-info DTO; every call is audited.
/// </summary>
public sealed class GetTenantActorQueryHandler(
    ITenantRepository tenants,
    IB2BHandshakeVerifier handshakeVerifier,
    IInternalAccessAuditWriter auditWriter,
    ILogger<GetTenantActorQueryHandler> log)
    : IRequestHandler<GetTenantActorQuery, Result<TenantActorResponse>>
{
    /// <inheritdoc/>
    public async Task<Result<TenantActorResponse>> Handle(
        GetTenantActorQuery query, CancellationToken ct)
    {
        var tenant = await tenants
            .GetByIdAsync(TenantId.From(query.TargetTenantId), ct)
            .ConfigureAwait(false);

        if (tenant is null)
        {
            await RecordAndLogAsync(query, "NotFound", ct).ConfigureAwait(false);
            return Result.NotFound();
        }

        var hasHandshake = await handshakeVerifier
            .HasVerifiedHandshakeAsync(query.RequesterTenantId, query.TargetTenantId, ct)
            .ConfigureAwait(false);

        await RecordAndLogAsync(query, "Found", ct).ConfigureAwait(false);

        return Result.Success(new TenantActorResponse(
            TenantId:                          tenant.Id.Value,
            TenantType:                        tenant.TenantType.ToString(),
            DisplayName:                       tenant.Name.Value,
            HasVerifiedHandshakeWithRequester: hasHandshake));
    }

    private async Task RecordAndLogAsync(
        GetTenantActorQuery query, string result, CancellationToken ct)
    {
        log.LogInformation(
            "InternalActorLookup requester={RequesterTenantId} target={TargetTenantId} result={Result}",
            query.RequesterTenantId, query.TargetTenantId, result);

        try
        {
            await auditWriter
                .RecordAsync(query.RequesterTenantId, query.TargetTenantId, result, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            log.LogCritical(ex,
                "AUDIT WRITE FAILED — InternalActorLookup requester={RequesterTenantId} target={TargetTenantId}",
                query.RequesterTenantId, query.TargetTenantId);
        }
    }
}
