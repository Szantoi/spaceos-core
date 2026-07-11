// SpaceOS.Kernel.Application/Tools/Queries/TenantSummaryDto.cs
namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Tenant aggregate summary for LLM tool consumption.</summary>
/// <param name="FlowEpicCount">Number of non-archived FlowEpics for the tenant.</param>
/// <param name="ActiveWorkstationCount">Number of non-archived WorkStations for the tenant.</param>
/// <param name="FacilityCount">Number of non-archived Facilities for the tenant.</param>
public sealed record TenantSummaryDto(
    int FlowEpicCount,
    int ActiveWorkstationCount,
    int FacilityCount);
