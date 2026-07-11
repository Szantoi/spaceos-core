// SpaceOS.Modules.Abstractions/Sync/INodeAuthService.cs
namespace SpaceOS.Modules.Abstractions.Sync;

/// <summary>
/// Issues and validates short-lived JWTs used for inter-node authentication during
/// SIP synchronisation.
/// </summary>
public interface INodeAuthService
{
    /// <summary>
    /// Issues a signed JWT that authorises <paramref name="tenantId"/> to call the
    /// node at <paramref name="nodeUrl"/>.
    /// </summary>
    /// <param name="tenantId">The tenant on whose behalf the token is issued.</param>
    /// <param name="nodeUrl">The target node URL to embed as the audience claim.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A compact-serialised JWT string.</returns>
    Task<string> IssueNodeJwtAsync(Guid tenantId, string nodeUrl, CancellationToken ct = default);

    /// <summary>
    /// Validates a previously issued node JWT and returns whether it is currently
    /// valid (signature, expiry, and audience all pass).
    /// </summary>
    /// <param name="token">The compact-serialised JWT to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the token is valid; otherwise <c>false</c>.</returns>
    Task<bool> ValidateNodeJwtAsync(string token, CancellationToken ct = default);
}
