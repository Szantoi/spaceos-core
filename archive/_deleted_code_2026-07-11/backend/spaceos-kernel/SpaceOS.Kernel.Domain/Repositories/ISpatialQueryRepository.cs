using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Read-model contract for 4D spatial timeline queries.
/// Implementation uses raw SQL (spec §5.4) joining SpatialElements, BvhNodes,
/// SpatialTaskLinks and AuditEvents tables.
/// </summary>
public interface ISpatialQueryRepository
{
    /// <summary>
    /// Returns the spatial contract snapshot at a given point in time.
    /// Each element shows the most recent FSM state at or before <paramref name="at"/>.
    /// </summary>
    /// <param name="physicalSpaceId">The physical space to query.</param>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="at">The point-in-time cutoff.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="offset">The zero-based offset for pagination.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A tuple of (items, totalCount) for the snapshot.</returns>
    Task<(IReadOnlyList<SpatialContractRow> Items, int TotalCount)> GetSnapshotAtAsync(
        Guid physicalSpaceId,
        Guid tenantId,
        DateTimeOffset at,
        int pageSize,
        int offset,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all timeline events for a physical space in chronological order.
    /// </summary>
    /// <param name="physicalSpaceId">The physical space to query.</param>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A list of timeline event rows.</returns>
    Task<IReadOnlyList<SpatialTimelineRow>> GetTimelineEventsAsync(
        Guid physicalSpaceId,
        Guid tenantId,
        CancellationToken ct = default);
}

/// <summary>
/// Raw row returned by the spatial snapshot query. Mapped to DTO in the application layer.
/// </summary>
public sealed record SpatialContractRow(
    Guid ElementId,
    int MinX, int MinY, int MinZ,
    int MaxX, int MaxY, int MaxZ,
    string TradeType,
    string FsmStateAtT,
    DateTimeOffset ReachedAt);

/// <summary>
/// Raw row returned by the spatial timeline events query. Mapped to DTO in the application layer.
/// </summary>
public sealed record SpatialTimelineRow(
    DateTimeOffset OccurredAt,
    Guid ElementId,
    string TradeType,
    string FromState,
    string ToState);
