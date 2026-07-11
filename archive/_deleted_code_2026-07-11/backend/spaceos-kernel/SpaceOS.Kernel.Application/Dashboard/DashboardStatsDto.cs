// SpaceOS.Kernel.Application/Dashboard/DashboardStatsDto.cs

namespace SpaceOS.Kernel.Application.Dashboard;

/// <summary>
/// Data transfer object containing the aggregated system-wide counts
/// returned by the <c>GET /api/dashboard/stats</c> endpoint.
/// </summary>
/// <param name="TenantCount">Total number of tenants in the system.</param>
/// <param name="FacilityCount">Total number of facilities across all tenants.</param>
/// <param name="WorkStationCount">Total number of workstations across all tenants.</param>
/// <param name="ActiveWorkStationCount">Number of workstations currently in Active status.</param>
/// <param name="FlowEpicCount">Total number of flow epics across all tenants.</param>
/// <param name="AuditEventCount">Total number of audit events recorded in the system.</param>
public sealed record DashboardStatsDto(
    int TenantCount,
    int FacilityCount,
    int WorkStationCount,
    int ActiveWorkStationCount,
    int FlowEpicCount,
    int AuditEventCount);
