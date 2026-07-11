// SpaceOS.Kernel.Application/AuditLog/Queries/GetAuditEventsQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog.Specifications;

namespace SpaceOS.Kernel.Application.AuditLog.Queries;

/// <summary>
/// Handles <see cref="GetAuditEventsQuery"/>: returns a paged list of audit events
/// for a tenant projected to <see cref="AuditEventDto"/>.
/// </summary>
internal sealed class GetAuditEventsQueryHandler
    : IRequestHandler<GetAuditEventsQuery, Result<PagedList<AuditEventDto>>>
{
    private readonly IAuditEventRepository _repository;

    /// <summary>Initialises a new <see cref="GetAuditEventsQueryHandler"/>.</summary>
    /// <param name="repository">The audit event repository.</param>
    public GetAuditEventsQueryHandler(IAuditEventRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Executes the query and returns a paged result of audit event DTOs.</summary>
    public async Task<Result<PagedList<AuditEventDto>>> Handle(
        GetAuditEventsQuery request,
        CancellationToken ct)
    {
        var countSpec = new AuditEventsByTenantFilterSpec(request.TenantId, request.EventType, request.From, request.To);
        var totalCount = await _repository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new AuditEventsByTenantPagedSpec(
            request.TenantId, request.EventType, request.From, request.To, request.Page, request.PageSize);
        var events = await _repository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = events
            .Select(e => new AuditEventDto(
                e.Id,
                e.EventType,
                e.AggregateId,
                e.StateHash,
                e.OccurredAt,
                e.ActorId,
                e.SourceIp,
                e.SourceBrand))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<AuditEventDto>(items, request.Page, request.PageSize, totalCount));
    }
}
