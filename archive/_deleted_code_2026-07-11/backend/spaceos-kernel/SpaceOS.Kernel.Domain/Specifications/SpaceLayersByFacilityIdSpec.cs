using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Returns all <see cref="SpaceLayer"/> instances belonging to the specified facility.
/// </summary>
public sealed class SpaceLayersByFacilityIdSpec : Specification<SpaceLayer>
{
    public SpaceLayersByFacilityIdSpec(FacilityId facilityId)
    {
        Query.Where(sl => sl.FacilityId == facilityId);
    }
}
