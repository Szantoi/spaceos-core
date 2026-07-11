using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Returns all <see cref="FlowEpic"/> instances targeting the specified facility.
/// </summary>
public sealed class FlowEpicsByFacilityIdSpec : Specification<FlowEpic>
{
    public FlowEpicsByFacilityIdSpec(FacilityId facilityId)
    {
        Query.Where(e => e.TargetFacilityId == facilityId);
    }
}
