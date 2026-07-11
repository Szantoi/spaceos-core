namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Coordinates atomic transactions across multiple repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes made in the current transaction.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
