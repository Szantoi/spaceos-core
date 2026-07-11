// SpaceOS.Kernel.Domain/Specifications/FlowEpicsByFacilityPagedSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of <see cref="FlowEpic"/> aggregates targeting the specified facility.</summary>
public sealed class FlowEpicsByFacilityPagedSpec : PagedSpecification<FlowEpic>
{
    /// <summary>Initialises the specification with a facility filter and paging constraints.</summary>
    /// <param name="facilityId">The facility to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public FlowEpicsByFacilityPagedSpec(FacilityId facilityId, int page, int pageSize)
        : base(page, pageSize)
    {
        Query.Where(e => e.TargetFacilityId == facilityId);
    }
}
