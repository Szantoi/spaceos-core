using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Facilities.Queries;

/// <summary>
/// Handles <see cref="GetFacilityByIdQuery"/>: retrieves a facility by identifier and
/// projects the result to a <see cref="FacilityDto"/>.
/// </summary>
public class GetFacilityByIdQueryHandler : IRequestHandler<GetFacilityByIdQuery, Result<FacilityDto>>
{
    private readonly IFacilityRepository _facilityRepository;

    /// <summary>
    /// Initialises a new <see cref="GetFacilityByIdQueryHandler"/>.
    /// </summary>
    /// <param name="facilityRepository">Repository for facility queries.</param>
    public GetFacilityByIdQueryHandler(IFacilityRepository facilityRepository)
    {
        ArgumentNullException.ThrowIfNull(facilityRepository);
        _facilityRepository = facilityRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<FacilityDto>> Handle(GetFacilityByIdQuery request, CancellationToken ct)
    {
        var facility = await _facilityRepository
            .GetByIdAsync(FacilityId.From(request.FacilityId), ct)
            .ConfigureAwait(false);

        if (facility is null)
        {
            return Result.NotFound();
        }

        return Result.Success(new FacilityDto(
            facility.Id.Value,
            facility.Name.Value,
            facility.TenantId.Value));
    }
}
