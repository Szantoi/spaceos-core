// SpaceOS.Kernel.Application/Dashboard/Queries/GetDashboardStatsQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Dashboard;

namespace SpaceOS.Kernel.Application.Dashboard.Queries;

/// <summary>
/// Handles <see cref="GetDashboardStatsQuery"/>: delegates to the cross-aggregate
/// <see cref="IDashboardStatsQuery"/> read model and maps the result to
/// <see cref="DashboardStatsDto"/>.
/// </summary>
internal sealed class GetDashboardStatsQueryHandler
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IDashboardStatsQuery _dashboardStatsQuery;

    /// <summary>Initialises a new <see cref="GetDashboardStatsQueryHandler"/>.</summary>
    /// <param name="dashboardStatsQuery">The cross-aggregate dashboard stats read model.</param>
    public GetDashboardStatsQueryHandler(IDashboardStatsQuery dashboardStatsQuery)
    {
        ArgumentNullException.ThrowIfNull(dashboardStatsQuery);
        _dashboardStatsQuery = dashboardStatsQuery;
    }

    /// <summary>Executes the query and returns the dashboard stats DTO.</summary>
    /// <param name="request">The query (no parameters required).</param>
    /// <param name="ct">A token that can be used to cancel the operation.</param>
    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request,
        CancellationToken ct)
    {
        var stats = await _dashboardStatsQuery.QueryAsync(ct).ConfigureAwait(false);

        return Result<DashboardStatsDto>.Success(new DashboardStatsDto(
            stats.TenantCount,
            stats.FacilityCount,
            stats.WorkStationCount,
            stats.ActiveWorkStationCount,
            stats.FlowEpicCount,
            stats.AuditEventCount));
    }
}
