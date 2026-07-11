// SpaceOS.Kernel.Application/Tools/Queries/ListWorkStationsQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Handles <see cref="ListWorkStationsQuery"/>: returns a paginated list of WorkStations for the authenticated tenant.</summary>
internal sealed class ListWorkStationsQueryHandler
    : IRequestHandler<ListWorkStationsQuery, Result<PagedList<WorkStationSummaryDto>>>
{
    private readonly IWorkStationRepository _repository;

    /// <summary>Initialises the handler.</summary>
    /// <param name="repository">The workstation repository.</param>
    public ListWorkStationsQueryHandler(IWorkStationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedList<WorkStationSummaryDto>>> Handle(
        ListWorkStationsQuery request,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var page     = Math.Max(1, request.Page);
        var tenantId = TenantId.From(request.TenantId);

        var countSpec = new WorkStationsByTenantIdSpec(tenantId);
        var total     = await _repository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new WorkStationsByTenantPagedSpec(tenantId, page, pageSize);
        var stations  = await _repository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = stations
            .Select(ws => new WorkStationSummaryDto(
                ws.Id.Value,
                ws.Name.Value,
                ws.Status.ToString(),
                ws.FacilityId.Value))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<WorkStationSummaryDto>(items, page, pageSize, total));
    }
}
