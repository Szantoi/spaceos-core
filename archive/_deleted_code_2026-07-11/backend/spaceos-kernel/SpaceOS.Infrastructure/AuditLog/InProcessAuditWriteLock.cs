// SpaceOS.Infrastructure/AuditLog/InProcessAuditWriteLock.cs

using System.Collections.Concurrent;
using SpaceOS.Kernel.Application.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// In-process serialization lock for the audit event hash chain, backed by a per-tenant
/// <see cref="SemaphoreSlim"/>.
///
/// <para>
/// <strong>Single-instance constraint:</strong> This implementation relies on a
/// process-local <see cref="ConcurrentDictionary{TKey,TValue}"/> of semaphores.
/// It correctly serialises concurrent audit appends <em>within a single process</em>,
/// but provides <em>no cross-process exclusion</em>. Two API instances running concurrently
/// would each hold their own semaphore and could produce a forked hash chain.
/// </para>
///
/// <para>
/// <strong>Permitted deployment scenarios:</strong>
/// <list type="bullet">
///   <item>Single-container development deployments (Docker Compose, local run).</item>
///   <item>SQLite-backed test environments — <see cref="RepositoryTestBase"/> and
///   <see cref="SpaceOsApiFactory"/> both use this lock because SQLite cannot host
///   a PostgreSQL advisory lock.</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Production guidance:</strong> Use <see cref="PostgresAdvisoryAuditWriteLock"/>
/// for any deployment where more than one API process may write to the same tenant's
/// audit chain concurrently.  See <c>docs/adr/ADR-005-advisory-lock-audit-chain.md</c>
/// for the full architectural decision record.
/// </para>
/// </summary>
internal sealed class InProcessAuditWriteLock : IAuditWriteLock
{
    /// <summary>
    /// Per-tenant semaphores. Static so all instances within the same process share
    /// the same lock state — this is intentional and load-tested in
    /// <c>AuditEventRaceConditionTests</c>.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    /// <inheritdoc/>
    /// <remarks>
    /// Acquires or creates a <see cref="SemaphoreSlim(1,1)"/> keyed by <paramref name="tenantId"/>.
    /// The returned <see cref="IAsyncDisposable"/> releases the semaphore on dispose, so callers
    /// must always <c>await using</c> the handle.
    /// </remarks>
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
