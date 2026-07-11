// SpaceOS.Kernel.Domain/Repositories/ITransactionManager.cs
namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Provides explicit transaction demarcation for use cases that require atomicity
/// beyond a single <c>IUnitOfWork.SaveChangesAsync</c> call (e.g., hash-chain + insert).
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Begins a new database transaction.
    /// Disposing the returned handle rolls back the transaction if
    /// <see cref="CommitAsync"/> has not been called.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that rolls back on disposal.</returns>
    Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commits the current transaction, making all pending changes durable.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back the current transaction, discarding all pending changes.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    Task RollbackAsync(CancellationToken ct = default);
}
