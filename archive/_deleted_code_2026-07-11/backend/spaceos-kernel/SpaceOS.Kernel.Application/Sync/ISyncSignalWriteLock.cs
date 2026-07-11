// SpaceOS.Kernel.Application/Sync/ISyncSignalWriteLock.cs
namespace SpaceOS.Kernel.Application.Sync;

/// <summary>
/// Provides a per-tenant serialization lock for appending to the sync-signal hash chain.
/// Prevents concurrent writes that would produce a forked chain.
/// </summary>
public interface ISyncSignalWriteLock
{
    /// <summary>
    /// Acquires an exclusive lock for the given tenant's sync-signal chain.
    /// Disposing the returned handle releases the lock.
    /// </summary>
    /// <param name="tenantId">The tenant whose sync chain is being appended to.</param>
    /// <param name="ct">A token to cancel the acquisition.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that releases the lock when disposed.</returns>
    Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default);
}
