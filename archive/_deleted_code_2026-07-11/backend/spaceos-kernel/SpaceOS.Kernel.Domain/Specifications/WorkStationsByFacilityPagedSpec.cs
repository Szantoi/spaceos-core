// SpaceOS.Kernel.Domain/Specifications/WorkStationsByFacilityPagedSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of <see cref="WorkStation"/> aggregates belonging to the specified facility.</summary>
public sealed class WorkStationsByFacilityPagedSpec : PagedSpecification<WorkStation>
{
    /// <summary>Initialises the specification with a facility filter and paging constraints.</summary>
    /// <param name="facilityId">The facility to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public WorkStationsByFacilityPagedSpec(FacilityId facilityId, int page, int pageSize)
        : base(page, pageSize)
    {
        Query.Where(ws => ws.FacilityId == facilityId);
    }
}
