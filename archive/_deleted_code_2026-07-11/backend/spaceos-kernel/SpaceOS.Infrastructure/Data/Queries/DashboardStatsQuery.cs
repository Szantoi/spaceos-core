// SpaceOS.Infrastructure/Data/Queries/DashboardStatsQuery.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Dashboard;

namespace SpaceOS.Infrastructure.Data.Queries;

/// <summary>
/// EF Core implementation of <see cref="IDashboardStatsQuery"/>.
/// Issues a single SQL query that aggregates all six counts in one database round-trip.
/// </summary>
internal sealed class DashboardStatsQuery : IDashboardStatsQuery
{
    private readonly AppDbContext _context;

    /// <summary>Initialises a new <see cref="DashboardStatsQuery"/>.</summary>
    /// <param name="context">The application database context.</param>
    public DashboardStatsQuery(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<DashboardStats> QueryAsync(CancellationToken ct)
    {
        // Single round-trip: all six counts computed in one SQL SELECT.
        // WorkStation.Status is stored as a string column ("Active") per the EF configuration.
        var results = await _context.Database
            .SqlQuery<DashboardStatsRow>(
                $"""
                SELECT
                    CAST((SELECT COUNT(*) FROM "Tenants") AS INTEGER)                                    AS "TenantCount",
                    CAST((SELECT COUNT(*) FROM "Facilities") AS INTEGER)                                 AS "FacilityCount",
                    CAST((SELECT COUNT(*) FROM "WorkStations") AS INTEGER)                               AS "WorkStationCount",
                    CAST((SELECT COUNT(*) FROM "WorkStations" WHERE "Status" = 'Active') AS INTEGER)     AS "ActiveWorkStationCount",
                    CAST((SELECT COUNT(*) FROM "FlowEpics") AS INTEGER)                                  AS "FlowEpicCount",
                    CAST((SELECT COUNT(*) FROM "AuditEvents") AS INTEGER)                                AS "AuditEventCount"
                """)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var row = results[0];

        return new DashboardStats(
            row.TenantCount,
            row.FacilityCount,
            row.WorkStationCount,
            row.ActiveWorkStationCount,
            row.FlowEpicCount,
            row.AuditEventCount);
    }

    // Private projection type — used only to receive the raw SQL result set.
    private sealed record DashboardStatsRow(
        int TenantCount,
        int FacilityCount,
        int WorkStationCount,
        int ActiveWorkStationCount,
        int FlowEpicCount,
        int AuditEventCount);
}
