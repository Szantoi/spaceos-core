namespace SpaceOS.Kernel.Application.Internal.Ports;

/// <summary>
/// Append-only audit writer for internal actor directory lookups (SEC-S-09).
/// Every call to GET /api/internal/tenants/{id} is recorded regardless of outcome.
/// </summary>
public interface IInternalAccessAuditWriter
{
    /// <summary>
    /// Records a single internal directory lookup access event.
    /// </summary>
    /// <param name="requesterTenantId">The tenant that performed the lookup.</param>
    /// <param name="targetTenantId">The tenant that was looked up.</param>
    /// <param name="result">"Found" | "NotFound"</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task RecordAsync(
        Guid requesterTenantId,
        Guid targetTenantId,
        string result,
        CancellationToken ct);
}
