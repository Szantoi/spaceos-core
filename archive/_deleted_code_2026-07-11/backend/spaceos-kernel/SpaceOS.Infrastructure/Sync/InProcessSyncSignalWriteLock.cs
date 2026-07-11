// SpaceOS.Infrastructure/Sync/InProcessSyncSignalWriteLock.cs
using System.Collections.Concurrent;
using SpaceOS.Kernel.Application.Sync;

namespace SpaceOS.Infrastructure.Sync;

/// <summary>
/// In-process serialization lock backed by a per-tenant <see cref="SemaphoreSlim"/>.
/// Suitable for single-instance deployments and SQLite (development).
/// For multi-instance production deployments, replace with a distributed lock (e.g., PostgreSQL advisory lock).
/// </summary>
internal sealed class InProcessSyncSignalWriteLock : ISyncSignalWriteLock
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        return new SemaphoreReleaser(semaphore);
    }

    private sealed class SemaphoreReleaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
