// SpaceOS.Kernel.Domain/Services/IWormStorageService.cs

namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Write-once, read-never WORM storage abstraction for audit hash chain blocks.
/// Implementations must connect with a role that has ONLY INSERT privileges — no SELECT, UPDATE or DELETE.
/// </summary>
/// <remarks>
/// SEC-03: The underlying DB role (<c>spaceos_audit_worm</c>) must be granted INSERT only.
/// SEC-07: The connection string must come from the <c>AUDIT_SINK_CONNECTION_STRING</c> environment
/// variable and must NOT appear in <c>appsettings.json</c>.
/// </remarks>
public interface IWormStorageService
{
    /// <summary>
    /// Appends an audit hash block to the immutable store.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the audit chain.</param>
    /// <param name="blockIndex">Monotonically increasing block sequence number within the tenant chain.</param>
    /// <param name="hash">The SHA-256 (or other algorithm) state hash for this block.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AppendAsync(Guid tenantId, long blockIndex, string hash, CancellationToken ct = default);
}
