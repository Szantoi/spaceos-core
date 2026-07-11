// SpaceOS.Infrastructure/Data/Repositories/SpatialQueryRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISpatialQueryRepository"/>.
/// Uses raw SQL for 4D spatial timeline queries (spec §5.4).
/// </summary>
public class SpatialQueryRepository : ISpatialQueryRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="SpatialQueryRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public SpatialQueryRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<SpatialContractRow> Items, int TotalCount)> GetSnapshotAtAsync(
        Guid physicalSpaceId,
        Guid tenantId,
        DateTimeOffset at,
        int pageSize,
        int offset,
        CancellationToken ct = default)
    {
        // Count query — uses a CTE to get distinct element count at point-in-time.
        var countRows = await _dbContext.Database
            .SqlQuery<CountRow>(
                $"""
                SELECT CAST(COUNT(*) AS INTEGER) AS "Value"
                FROM (
                    SELECT DISTINCT ON (se."Id") se."Id"
                    FROM "SpatialElements" se
                    JOIN "BvhNodes"         bn  ON bn."Id" = se."BvhLeafId"
                    JOIN "SpatialTaskLinks" stl ON stl."SpatialElementId" = se."Id"
                    JOIN "AuditEvents"      ae  ON ae."Payload"->>'flowTaskId' = stl."FlowTaskId"::text
                    WHERE se."BvhLeafId" IN (
                        SELECT bn2."Id" FROM "BvhNodes" bn2
                        WHERE bn2."PhysicalSpaceId" = {physicalSpaceId}
                    )
                      AND se."TenantId"   = {tenantId}
                      AND se."IsArchived"  = false
                      AND ae."EventType"   = 'FlowTaskStateChanged'
                      AND ae."OccurredAt" <= {at}
                      AND try_cast_uuid(ae."Payload"->>'flowTaskId') IS NOT NULL
                    ORDER BY se."Id", ae."OccurredAt" DESC
                ) sub
                """)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalCount = countRows.Count > 0 ? countRows[0].Value : 0;

        // Data query — paginated snapshot at point-in-time.
        var rows = await _dbContext.Database
            .SqlQuery<SpatialContractRow>(
                $"""
                SELECT DISTINCT ON (se."Id")
                    se."Id"                          AS "ElementId",
                    bn."MinX", bn."MinY", bn."MinZ",
                    bn."MaxX", bn."MaxY", bn."MaxZ",
                    CAST(se."TradeType" AS TEXT)      AS "TradeType",
                    ae."Payload"->>'newState'         AS "FsmStateAtT",
                    ae."OccurredAt"                   AS "ReachedAt"
                FROM "SpatialElements" se
                JOIN "BvhNodes"         bn  ON bn."Id" = se."BvhLeafId"
                JOIN "SpatialTaskLinks" stl ON stl."SpatialElementId" = se."Id"
                JOIN "AuditEvents"      ae  ON ae."Payload"->>'flowTaskId' = stl."FlowTaskId"::text
                WHERE se."BvhLeafId" IN (
                    SELECT bn2."Id" FROM "BvhNodes" bn2
                    WHERE bn2."PhysicalSpaceId" = {physicalSpaceId}
                )
                  AND se."TenantId"   = {tenantId}
                  AND se."IsArchived"  = false
                  AND ae."EventType"   = 'FlowTaskStateChanged'
                  AND ae."OccurredAt" <= {at}
                  AND try_cast_uuid(ae."Payload"->>'flowTaskId') IS NOT NULL
                ORDER BY se."Id", ae."OccurredAt" DESC
                LIMIT {pageSize} OFFSET {offset}
                """)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (rows.AsReadOnly(), totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SpatialTimelineRow>> GetTimelineEventsAsync(
        Guid physicalSpaceId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var rows = await _dbContext.Database
            .SqlQuery<SpatialTimelineRow>(
                $"""
                SELECT
                    ae."OccurredAt",
                    se."Id"                               AS "ElementId",
                    CAST(se."TradeType" AS TEXT)           AS "TradeType",
                    ae."Payload"->>'previousState'        AS "FromState",
                    ae."Payload"->>'newState'              AS "ToState"
                FROM "AuditEvents" ae
                JOIN "SpatialTaskLinks" stl
                    ON stl."FlowTaskId" = try_cast_uuid(ae."Payload"->>'flowTaskId')
                JOIN "SpatialElements"  se ON se."Id" = stl."SpatialElementId"
                WHERE se."BvhLeafId" IN (
                    SELECT bn."Id" FROM "BvhNodes" bn
                    WHERE bn."PhysicalSpaceId" = {physicalSpaceId}
                )
                  AND se."TenantId"   = {tenantId}
                  AND se."IsArchived"  = false
                  AND ae."EventType"   = 'FlowTaskStateChanged'
                  AND try_cast_uuid(ae."Payload"->>'flowTaskId') IS NOT NULL
                ORDER BY ae."OccurredAt" ASC
                """)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.AsReadOnly();
    }

    /// <summary>Helper record for raw SQL count queries.</summary>
    private sealed record CountRow(int Value);
}
