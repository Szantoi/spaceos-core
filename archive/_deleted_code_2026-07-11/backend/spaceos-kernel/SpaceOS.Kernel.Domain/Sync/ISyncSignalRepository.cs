// SpaceOS.Kernel.Domain/Sync/ISyncSignalRepository.cs
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Sync;

/// <summary>
/// Persistence contract for the <see cref="SyncSignal"/> aggregate root.
/// </summary>
public interface ISyncSignalRepository
{
    /// <summary>
    /// Returns the <see cref="SyncSignal"/> matching the given client-generated idempotency key
    /// for the specified tenant, or <see langword="null"/> if no such signal exists.
    /// </summary>
    /// <param name="tenantId">The tenant that emitted the signal.</param>
    /// <param name="clientSignalId">The client-generated UUID used for idempotent delivery.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<SyncSignal?> GetByClientSignalIdAsync(TenantId tenantId, Guid clientSignalId, CancellationToken ct = default);

    /// <summary>
    /// Stages a new <see cref="SyncSignal"/> for insertion.
    /// Changes are committed via <c>IUnitOfWork.SaveChangesAsync</c>.
    /// </summary>
    /// <param name="signal">The signal to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(SyncSignal signal, CancellationToken ct = default);

    /// <summary>
    /// Returns the <see cref="SyncSignal.StateHash"/> of the most recently occurred signal
    /// for the given tenant, or <see cref="SyncConstants.GenesisHash"/> if none exists.
    /// Must be called within the write-lock scope to prevent hash-chain forks.
    /// </summary>
    /// <param name="tenantId">The tenant whose chain tail to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<string> GetLastHashAsync(TenantId tenantId, CancellationToken ct = default);
}
