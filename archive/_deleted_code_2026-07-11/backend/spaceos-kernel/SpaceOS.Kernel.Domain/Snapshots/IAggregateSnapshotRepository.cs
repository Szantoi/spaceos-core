// SpaceOS.Kernel.Domain/Snapshots/IAggregateSnapshotRepository.cs

namespace SpaceOS.Kernel.Domain.Snapshots;

/// <summary>
/// Append-only repository for <see cref="AggregateSnapshot"/> records.
/// No update or delete operations are exposed — snapshots are immutable once written.
/// </summary>
public interface IAggregateSnapshotRepository
{
    /// <summary>
    /// Appends a new snapshot to the store.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(AggregateSnapshot snapshot, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent snapshot for the given aggregate, or <see langword="null"/> if none exists.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to query.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AggregateSnapshot?> GetLatestAsync(Guid aggregateId, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent snapshot for the given aggregate taken at or before
    /// <paramref name="at"/>, or <see langword="null"/> if none exists within that window.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to query.</param>
    /// <param name="at">The upper-bound timestamp (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AggregateSnapshot?> GetAtTimestampAsync(Guid aggregateId, DateTimeOffset at, CancellationToken ct = default);

    /// <summary>
    /// Returns all snapshots for the given aggregate ordered by ascending version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to query.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<AggregateSnapshot>> ListByAggregateAsync(Guid aggregateId, CancellationToken ct = default);
}
