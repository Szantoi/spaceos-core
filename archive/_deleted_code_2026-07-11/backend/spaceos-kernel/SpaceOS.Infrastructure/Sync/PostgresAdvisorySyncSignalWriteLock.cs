// SpaceOS.Infrastructure/Sync/PostgresAdvisorySyncSignalWriteLock.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Application.Sync;

namespace SpaceOS.Infrastructure.Sync;

/// <summary>
/// PostgreSQL advisory lock-based serialization for the sync-signal hash chain.
/// Uses <c>pg_try_advisory_xact_lock</c> per tenant to prevent concurrent appends
/// across multiple API instances (distributed-lock-safe).
/// </summary>
/// <remarks>
/// Lock key space: uses an offset of <c>0x5398_0000</c> added to the tenant hash so that
/// sync-signal locks never collide with audit-event locks (offset <c>0x0000_0000</c>).
/// </remarks>
internal sealed class PostgresAdvisorySyncSignalWriteLock : ISyncSignalWriteLock
{
    /// <summary>
    /// Offset added to the tenant hash to isolate sync-signal advisory locks from
    /// audit-event advisory locks in the same PostgreSQL session.
    /// </summary>
    private const long LockKeyOffset = 0x5398_0000L;

    private readonly AppDbContext _context;

    /// <summary>Initialises a new <see cref="PostgresAdvisorySyncSignalWriteLock"/>.</summary>
    /// <param name="context">The application database context used to execute advisory lock SQL.</param>
    public PostgresAdvisorySyncSignalWriteLock(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Advisory lock key: lower 32 bits of tenant GUID hash + offset to avoid collision
        // with PostgresAdvisoryAuditWriteLock which uses the same hash without offset.
        var lockKey = (long)(uint)(tenantId.GetHashCode()) + LockKeyOffset;

        // Spin until lock acquired (pg_try_advisory_xact_lock returns false if contended).
        bool acquired = false;
        while (!acquired)
        {
            ct.ThrowIfCancellationRequested();
            var result = await _context.Database
                .SqlQueryRaw<bool>("SELECT pg_try_advisory_xact_lock({0}) AS \"Value\"", lockKey)
                .FirstAsync(ct)
                .ConfigureAwait(false);
            acquired = result;
            if (!acquired)
                await Task.Delay(10, ct).ConfigureAwait(false);
        }

        // Advisory xact lock is released automatically at end of transaction.
        return NullDisposable.Instance;
    }

    private sealed class NullDisposable : IAsyncDisposable
    {
        internal static readonly NullDisposable Instance = new();

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
