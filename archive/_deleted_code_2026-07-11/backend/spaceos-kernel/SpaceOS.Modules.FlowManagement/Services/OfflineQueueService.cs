// SpaceOS.Modules.FlowManagement/Services/OfflineQueueService.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.FlowManagement.Domain;
using SpaceOS.Modules.FlowManagement.Infrastructure;

namespace SpaceOS.Modules.FlowManagement.Services;

/// <summary>
/// Manages the offline sync queue for the FlowManagement module.
/// Queue items represent pending operations that must be delivered to the Kernel.
/// </summary>
public sealed class OfflineQueueService
{
    private readonly ModulesDbContext _db;

    /// <summary>
    /// Initialises a new instance of <see cref="OfflineQueueService"/>.
    /// </summary>
    /// <param name="db">The modules database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is <c>null</c>.</exception>
    public OfflineQueueService(ModulesDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>
    /// Creates a new queue item with the given payload and persists it.
    /// </summary>
    /// <param name="tenantId">The tenant that owns this queue item.</param>
    /// <param name="payload">The serialised operation payload. Must not be null or whitespace.</param>
    /// <param name="clientSignalId">The client-generated idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task EnqueueAsync(Guid tenantId, string payload, Guid clientSignalId, CancellationToken ct)
    {
        var item = OfflineSyncQueueItem.Create(tenantId, payload, clientSignalId);
        await _db.OfflineSyncQueue.AddAsync(item, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a batch of non-expired queue items ordered by creation time (oldest first).
    /// </summary>
    /// <param name="batchSize">The maximum number of items to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of pending queue items.</returns>
    public async Task<IReadOnlyList<OfflineSyncQueueItem>> GetPendingAsync(int batchSize, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        return await _db.OfflineSyncQueue
            .AsNoTracking()
            .Where(i => i.ExpiresAt > now)
            .OrderBy(i => i.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a queue item by its identifier.
    /// No-ops silently if the item no longer exists.
    /// </summary>
    /// <param name="id">The identifier of the queue item to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RemoveAsync(Guid id, CancellationToken ct)
    {
        var item = await _db.OfflineSyncQueue
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            .ConfigureAwait(false);

        if (item is null)
            return;

        _db.OfflineSyncQueue.Remove(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Calculates the exponential back-off delay for a retry attempt.
    /// The delay is capped at 60 seconds: <c>min(2^retryCount, 60)</c> seconds.
    /// </summary>
    /// <param name="retryCount">The zero-based retry attempt number.</param>
    /// <returns>The back-off delay to wait before the next attempt.</returns>
    public static TimeSpan GetBackoffDelay(int retryCount)
    {
        var seconds = Math.Min(Math.Pow(2, retryCount), 60);
        return TimeSpan.FromSeconds(seconds);
    }
}
