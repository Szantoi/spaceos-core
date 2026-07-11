// SpaceOS.Kernel.Application/AuditLog/IAuditWriteLock.cs

namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Provides a per-tenant serialization lock for appending to the audit event chain.
/// Prevents concurrent writes that would produce a forked hash chain.
/// </summary>
public interface IAuditWriteLock
{
    /// <summary>
    /// Acquires an exclusive lock for the given tenant.
    /// Disposing the returned handle releases the lock.
    /// </summary>
    /// <param name="tenantId">The tenant whose audit chain is being appended to.</param>
    /// <param name="ct">A token to cancel the acquisition.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that releases the lock when disposed.</returns>
    Task<IAsyncDisposable> AcquireAsync(Guid tenantId, CancellationToken ct = default);
}
