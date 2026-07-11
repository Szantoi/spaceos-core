// SpaceOS.Kernel.Application/Common/IGenesisHashProvider.cs

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Provides the deployment-time genesis hash that anchors the audit event chain for a tenant.
/// The genesis hash is used as the <c>PreviousHash</c> of the very first audit event recorded.
/// </summary>
public interface IGenesisHashProvider
{
    /// <summary>
    /// Returns the genesis hash value that anchors the audit chain.
    /// Implementations may generate and cache the value on first access.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 64-character lowercase hex string.</returns>
    Task<string> GetGenesisHashAsync(CancellationToken ct = default);
}
