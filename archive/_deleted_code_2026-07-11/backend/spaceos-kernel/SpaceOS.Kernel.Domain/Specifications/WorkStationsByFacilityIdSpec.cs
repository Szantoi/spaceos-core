using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Specification that filters <see cref="WorkStation"/> entities by their owning <see cref="FacilityId"/>.
/// </summary>
public class WorkStationsByFacilityIdSpec : Specification<WorkStation>
{
    /// <summary>
    /// Initialises a new <see cref="WorkStationsByFacilityIdSpec"/> for the given facility.
    /// </summary>
    /// <param name="facilityId">The facility identifier to filter by.</param>
    public WorkStationsByFacilityIdSpec(FacilityId facilityId)
    {
        Query.Where(ws => ws.FacilityId == facilityId);
    }
}
