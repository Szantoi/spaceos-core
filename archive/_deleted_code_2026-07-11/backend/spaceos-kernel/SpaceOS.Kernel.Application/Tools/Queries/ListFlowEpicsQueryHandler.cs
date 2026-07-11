// SpaceOS.Kernel.Application/Tools/Queries/ListFlowEpicsQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Handles <see cref="ListFlowEpicsQuery"/>: returns a paginated list of FlowEpics for the authenticated tenant.</summary>
internal sealed class ListFlowEpicsQueryHandler
    : IRequestHandler<ListFlowEpicsQuery, Result<PagedList<FlowEpicSummaryDto>>>
{
    private readonly IFlowEpicRepository _repository;

    /// <summary>Initialises the handler.</summary>
    /// <param name="repository">The flow epic repository.</param>
    public ListFlowEpicsQueryHandler(IFlowEpicRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedList<FlowEpicSummaryDto>>> Handle(
        ListFlowEpicsQuery request,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var page     = Math.Max(1, request.Page);
        var tenantId = TenantId.From(request.TenantId);

        var countSpec = new FlowEpicsByTenantIdSpec(tenantId);
        var total     = await _repository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new FlowEpicsByTenantPagedSpec(tenantId, page, pageSize);
        var epics     = await _repository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = epics
            .Select(e => new FlowEpicSummaryDto(
                e.Id.Value,
                e.Title.Value,
                e.Phase.ToString(),
                e.TargetFacilityId.Value))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<FlowEpicSummaryDto>(items, page, pageSize, total));
    }
}
