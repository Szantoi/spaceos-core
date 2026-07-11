// SpaceOS.Kernel.Domain/Repositories/ITenantHandshakeAllowlistRepository.cs
using SpaceOS.Kernel.Domain.DTOs;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Repository interface for querying <see cref="Entities.TenantHandshakeAllowlist"/> entries.
/// </summary>
public interface ITenantHandshakeAllowlistRepository
{
    /// <summary>
    /// Returns the allowed host tenants for the given guest tenant, projected with tenant names.
    /// Results are capped at 20 entries (SEC-P3CP-08).
    /// </summary>
    /// <param name="guestTenantId">The guest tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of allowed host DTOs.</returns>
    Task<IReadOnlyList<AllowedHostDto>> GetAllowedHostsAsync(Guid guestTenantId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the given guest tenant is allowed to handshake with the given host tenant.
    /// </summary>
    /// <param name="guestTenantId">The guest tenant identifier.</param>
    /// <param name="hostTenantId">The host tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if an allowlist entry exists; otherwise <c>false</c>.</returns>
    Task<bool> IsAllowedAsync(Guid guestTenantId, Guid hostTenantId, CancellationToken ct = default);
}
