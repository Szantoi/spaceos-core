// SpaceOS.Kernel.Application/Tools/Queries/ListFacilitiesQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Handles <see cref="ListFacilitiesQuery"/>: returns a paginated list of Facilities for the authenticated tenant.</summary>
internal sealed class ListFacilitiesQueryHandler
    : IRequestHandler<ListFacilitiesQuery, Result<PagedList<FacilitySummaryDto>>>
{
    private readonly IFacilityRepository _repository;

    /// <summary>Initialises the handler.</summary>
    /// <param name="repository">The facility repository.</param>
    public ListFacilitiesQueryHandler(IFacilityRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedList<FacilitySummaryDto>>> Handle(
        ListFacilitiesQuery request,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var page     = Math.Max(1, request.Page);
        var tenantId = TenantId.From(request.TenantId);

        var countSpec  = new FacilitiesByTenantIdSpec(tenantId);
        var total      = await _repository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec  = new FacilitiesByTenantPagedSpec(tenantId, page, pageSize);
        var facilities = await _repository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = facilities
            .Select(f => new FacilitySummaryDto(f.Id.Value, f.Name.Value))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<FacilitySummaryDto>(items, page, pageSize, total));
    }
}
