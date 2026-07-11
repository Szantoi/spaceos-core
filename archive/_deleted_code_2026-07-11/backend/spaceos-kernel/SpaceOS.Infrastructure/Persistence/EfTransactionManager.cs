// SpaceOS.Infrastructure/Persistence/EfTransactionManager.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ITransactionManager"/>.
/// Wraps <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade"/> to provide
/// explicit transaction demarcation for use cases that require atomicity beyond a single
/// <c>SaveChangesAsync</c> call.
/// </summary>
internal sealed class EfTransactionManager : ITransactionManager
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Initialises a new <see cref="EfTransactionManager"/>.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public EfTransactionManager(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        // NpgsqlRetryingExecutionStrategy forbids calling BeginTransactionAsync outside of
        // CreateExecutionStrategy().ExecuteAsync(...). The wrapper retries transient
        // connection failures during transaction open; the returned guard manages commit/rollback.
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _transaction = await _context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return new TransactionRollbackGuard(_transaction!);
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to roll back.");

        await _transaction.RollbackAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Rolls back the transaction on disposal if <see cref="CommitAsync"/> was not called.
    /// This ensures the transaction is always cleaned up even on exception paths.
    /// </summary>
    private sealed class TransactionRollbackGuard(IDbContextTransaction transaction) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
