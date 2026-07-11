// SpaceOS.Kernel.Application/Dashboard/Queries/GetDashboardStatsQuery.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Dashboard.Queries;

/// <summary>
/// Query that retrieves aggregated system-wide statistics for the dashboard.
/// All six counts are fetched in a single database round-trip.
/// </summary>
public sealed record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;
