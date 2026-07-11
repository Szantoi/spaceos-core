// SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Application.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// PostgreSQL advisory lock-based serialization for the audit event hash chain.
///
/// <para>
/// <strong>Purpose:</strong> The audit log maintains a SHA-256 hash chain where every event's
/// <c>PreviousHash</c> equals the <c>StateHash</c> of the event immediately before it.
/// If two API instances append to the same tenant's chain concurrently they will both read
/// the same tail hash and produce two events with an identical <c>PreviousHash</c> — a forked
/// chain that cannot be verified.  This lock prevents that race.
/// </para>
///
/// <para>
/// <strong>Mechanism:</strong> Begins an explicit transaction on <see cref="AuditDbContext"/>,
/// then calls <c>pg_try_advisory_xact_lock(key)</c> within that transaction.  The lock is
/// held until the caller disposes the returned handle, which commits (or rolls back on failure)
/// the transaction — releasing the advisory lock.  Because the lock, the tail-hash read, and
/// the event insert all share the same <see cref="AuditDbContext"/> connection and transaction,
/// there is no window for a concurrent writer to fork the chain.
/// </para>
///
/// <para>
/// <strong>KERNEL-070 fix:</strong> The previous implementation acquired the advisory lock on
/// <see cref="SpaceOS.Infrastructure.Data.AppDbContext"/> — a different connection than the
/// <see cref="AuditDbContext"/> used for reads and writes.  The lock was released as soon as
/// the implicit transaction for the <c>SELECT</c> completed, providing no actual serialization.
/// This fix moves the lock to <see cref="AuditDbContext"/> and wraps it in an explicit
/// transaction so the lock is held from acquisition through commit.
/// </para>
///
/// <para>
/// <strong>Multi-instance guarantee:</strong> Because the lock is held inside the database
/// server, it serialises appends across every API process connected to the same PostgreSQL
/// instance — regardless of the number of running containers or pods.
/// </para>
///
/// <para>
/// <strong>Spin strategy:</strong> <c>pg_try_advisory_xact_lock</c> returns <see langword="false"/>
/// immediately when the lock is held by another transaction (non-blocking variant). This
/// implementation spins with a 10 ms back-off to avoid overwhelming the database with
/// rapid retries. The spin is bounded by the caller's <see cref="CancellationToken"/>.
/// </para>
///
/// <para>
/// <strong>Limitations:</strong>
/// <list type="bullet">
///   <item>Requires a live PostgreSQL connection — incompatible with SQLite (development/test).
///   Use <see cref="InProcessAuditWriteLock"/> in those environments.</item>
///   <item>Advisory locks are not inherited by subtransactions (savepoints). The lock must be
///   acquired within the outermost transaction that writes the audit event.</item>
/// </list>
/// </para>
///
/// <para>
/// See <c>docs/adr/ADR-005-advisory-lock-audit-chain.md</c> for the full architectural decision record.
/// </para>
/// </summary>
internal sealed class PostgresAdvisoryAuditWriteLock : IAuditWriteLock
{
    private readonly AuditDbContext _context;

    /// <summary>Initialises a new <see cref="PostgresAdvisoryAuditWriteLock"/>.</summary>
    /// <param name="context">The audit database context — the same context used by
    /// <see cref="AuditEventRepository"/> and <see cref="AuditUnitOfWork"/>, ensuring
    /// that the advisory lock, the tail-hash read, and the event insert all share one
    /// connection and transaction.</param>
    public PostgresAdvisoryAuditWriteLock(AuditDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Begins an explicit transaction on <see cref="AuditDbContext"/>, then derives a stable
    /// <c>bigint</c> lock key from <paramref name="tenantId"/> using the first 8 bytes of its
    /// MD5 hash (SEC-06) and spins until <c>pg_try_advisory_xact_lock</c> returns
    /// <see langword="true"/>.
    /// </para>
    /// <para>
    /// The returned <see cref="IAsyncDisposable"/> commits the transaction on dispose,
    /// releasing the advisory lock.  If the transaction is in an aborted state (e.g. because
    /// <c>SaveChangesAsync</c> threw), the commit is skipped and the transaction is disposed
    /// which triggers an automatic rollback.
    /// </para>
    /// </remarks>
    public async Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default)
    {
        // NpgsqlRetryingExecutionStrategy forbids calling BeginTransactionAsync outside of
        // CreateExecutionStrategy().ExecuteAsync(...). Wrapping the acquire phase in ExecuteAsync
        // satisfies the check and retries any transient connection failure during lock acquisition.
        // The returned TransactionCommitter holds the open transaction and commits it when disposed.
        var strategy    = _context.Database.CreateExecutionStrategy();
        IDbContextTransaction? transaction = null;

        await strategy.ExecuteAsync(async () =>
        {
            // Begin an explicit transaction so that pg_try_advisory_xact_lock is held for the
            // entire read-compute-write sequence, not just the SELECT that acquires it.
            transaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct)
                .ConfigureAwait(false);

            try
            {
                // SEC-06: MD5-derived int64 key — avoids the 50% collision rate of hashtext(int4) at 10k+ tenants.
                // MD5 is used here exclusively as a hash function for key space distribution, not for security.
                var md5Bytes = MD5.HashData(Encoding.UTF8.GetBytes(tenantId.ToString()));
                var lockKey  = BitConverter.ToInt64(md5Bytes, 0);

                // Spin until lock acquired (pg_try_advisory_xact_lock returns false if contended)
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
            }
            catch
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
                transaction = null;
                throw;
            }
        }).ConfigureAwait(false);

        return new TransactionCommitter(transaction!);
    }

    /// <summary>
    /// Commits the wrapping transaction on dispose, releasing the advisory lock.
    /// If the transaction is aborted (e.g. constraint violation during save), the commit
    /// is skipped and dispose triggers an automatic rollback.
    /// </summary>
    private sealed class TransactionCommitter(IDbContextTransaction transaction) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await transaction.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                // Transaction may already be aborted (e.g. SaveChangesAsync threw).
                // Dispose will trigger an automatic rollback for aborted transactions.
            }
            finally
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
