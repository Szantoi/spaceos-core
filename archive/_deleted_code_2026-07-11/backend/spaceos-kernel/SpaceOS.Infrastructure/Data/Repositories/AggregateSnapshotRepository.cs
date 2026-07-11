// SpaceOS.Infrastructure/Data/Repositories/AggregateSnapshotRepository.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Snapshots;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAggregateSnapshotRepository"/>.
/// Append-only — no update or delete operations are exposed.
/// </summary>
internal sealed class AggregateSnapshotRepository : IAggregateSnapshotRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="AggregateSnapshotRepository"/>.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public AggregateSnapshotRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(AggregateSnapshot snapshot, CancellationToken ct = default)
    {
        await _context.AggregateSnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AggregateSnapshot?> GetLatestAsync(Guid aggregateId, CancellationToken ct = default)
    {
        return await _context.AggregateSnapshots
            .AsNoTracking()
            .Where(s => s.AggregateId == aggregateId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AggregateSnapshot?> GetAtTimestampAsync(Guid aggregateId, DateTimeOffset at, CancellationToken ct = default)
    {
        return await _context.AggregateSnapshots
            .AsNoTracking()
            .Where(s => s.AggregateId == aggregateId && s.SnapshotAt <= at)
            .OrderByDescending(s => s.SnapshotAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AggregateSnapshot>> ListByAggregateAsync(Guid aggregateId, CancellationToken ct = default)
    {
        return await _context.AggregateSnapshots
            .AsNoTracking()
            .Where(s => s.AggregateId == aggregateId)
            .OrderBy(s => s.Version)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
