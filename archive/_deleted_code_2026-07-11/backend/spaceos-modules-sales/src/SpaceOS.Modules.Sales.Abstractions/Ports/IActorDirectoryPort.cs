using Ardalis.Result;

namespace SpaceOS.Modules.Sales.Abstractions.Ports;

/// <summary>
/// Cross-module read port for resolving a platform tenant actor from the Kernel (ADR-039, D-12).
/// Implementation calls <c>GET http://127.0.0.1:5000/api/internal/tenants/{id}</c>.
/// SEC-S-09: returns minimal info only (no contact/billing data).
/// </summary>
public interface IActorDirectoryPort
{
    /// <summary>
    /// Resolves minimal actor information for a platform tenant.
    /// <paramref name="requesterTenantId"/> is sent so the Kernel can scope the lookup and
    /// audit the (requester→target) pair.
    /// </summary>
    Task<Result<ActorDirectoryEntry>> GetTenantActorAsync(
        Guid requesterTenantId, Guid platformTenantId, CancellationToken ct);
}

/// <summary>
/// Minimal actor info returned by the Kernel. SEC-S-09: no contact or billing data.
/// </summary>
/// <param name="TenantId">The platform tenant identifier.</param>
/// <param name="TenantType">Descriptor string (e.g. "manufacturer", "distributor").</param>
/// <param name="DisplayName">Public display name.</param>
/// <param name="HasVerifiedHandshakeWithRequester">
/// SEC-S-02: true if the Kernel already has a confirmed B2B handshake between requester and this tenant.
/// Drives <c>Customer.LinkStatus</c> (Verified vs Pending).
/// </param>
public sealed record ActorDirectoryEntry(
    Guid TenantId,
    string TenantType,
    string DisplayName,
    bool HasVerifiedHandshakeWithRequester);
