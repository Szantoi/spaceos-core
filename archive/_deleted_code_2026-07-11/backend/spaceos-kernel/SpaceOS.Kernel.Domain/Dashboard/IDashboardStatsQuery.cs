// SpaceOS.Kernel.Domain/Dashboard/IDashboardStatsQuery.cs

namespace SpaceOS.Kernel.Domain.Dashboard;

/// <summary>
/// Holds the aggregated counts returned by the dashboard stats read model.
/// </summary>
/// <param name="TenantCount">Total number of tenants in the system.</param>
/// <param name="FacilityCount">Total number of facilities across all tenants.</param>
/// <param name="WorkStationCount">Total number of workstations across all tenants.</param>
/// <param name="ActiveWorkStationCount">Number of workstations currently in <c>Active</c> status.</param>
/// <param name="FlowEpicCount">Total number of flow epics across all tenants.</param>
/// <param name="AuditEventCount">Total number of audit events recorded in the system.</param>
public sealed record DashboardStats(
    int TenantCount,
    int FacilityCount,
    int WorkStationCount,
    int ActiveWorkStationCount,
    int FlowEpicCount,
    int AuditEventCount);

/// <summary>
/// Cross-aggregate read-model query that returns a <see cref="DashboardStats"/> snapshot
/// using a single database round-trip.
/// </summary>
public interface IDashboardStatsQuery
{
    /// <summary>
    /// Executes the dashboard stats query and returns the current counts.
    /// </summary>
    /// <param name="ct">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="DashboardStats"/> record containing all six counts.</returns>
    Task<DashboardStats> QueryAsync(CancellationToken ct);
}
