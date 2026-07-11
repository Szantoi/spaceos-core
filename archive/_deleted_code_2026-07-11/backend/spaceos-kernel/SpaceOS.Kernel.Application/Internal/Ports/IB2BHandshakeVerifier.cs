namespace SpaceOS.Kernel.Application.Internal.Ports;

/// <summary>
/// Queries the Kernel handshake registry for a verified platform-level
/// relationship between two tenants (ADR-039 read path, SEC-S-09).
/// </summary>
public interface IB2BHandshakeVerifier
{
    /// <summary>
    /// Returns true if any allowlisted entry exists in either direction
    /// (tenantA → tenantB or tenantB → tenantA).
    /// </summary>
    /// <param name="tenantA">The first tenant in the pair.</param>
    /// <param name="tenantB">The second tenant in the pair.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> HasVerifiedHandshakeAsync(
        Guid tenantA,
        Guid tenantB,
        CancellationToken ct);
}
