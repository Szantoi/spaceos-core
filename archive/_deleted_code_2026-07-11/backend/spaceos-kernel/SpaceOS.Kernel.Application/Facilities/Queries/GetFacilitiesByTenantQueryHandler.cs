// SpaceOS.Kernel.Application/Facilities/Queries/GetFacilitiesByTenantQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Facilities.Queries;

/// <summary>
/// Handles <see cref="GetFacilitiesByTenantQuery"/>: returns a paged list of Facilities belonging to the given tenant.
/// </summary>
public sealed class GetFacilitiesByTenantQueryHandler
    : IRequestHandler<GetFacilitiesByTenantQuery, Result<PagedList<FacilityDto>>>
{
    private readonly IFacilityRepository _facilityRepository;

    /// <summary>Initialises a new <see cref="GetFacilitiesByTenantQueryHandler"/>.</summary>
    /// <param name="facilityRepository">The facility repository.</param>
    public GetFacilitiesByTenantQueryHandler(IFacilityRepository facilityRepository)
    {
        ArgumentNullException.ThrowIfNull(facilityRepository);
        _facilityRepository = facilityRepository;
    }

    /// <summary>Executes the query and returns a paged result of facility DTOs.</summary>
    public async Task<Result<PagedList<FacilityDto>>> Handle(
        GetFacilitiesByTenantQuery request,
        CancellationToken ct)
    {
        var tenantId = TenantId.From(request.TenantId);

        var countSpec = new FacilitiesByTenantIdSpec(tenantId);
        var totalCount = await _facilityRepository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new FacilitiesByTenantPagedSpec(tenantId, request.Page, request.PageSize);
        var facilities = await _facilityRepository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = facilities
            .Select(f => new FacilityDto(f.Id.Value, f.Name.Value, f.TenantId.Value))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<FacilityDto>(items, request.Page, request.PageSize, totalCount));
    }
}
