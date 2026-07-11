using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Application.Internal.Ports;

namespace SpaceOS.Infrastructure.Internal;

/// <summary>
/// EF Core implementation of <see cref="IInternalAccessAuditWriter"/>.
/// Appends a single <see cref="InternalAccessAuditEntry"/> row per call (SEC-S-09).
/// </summary>
internal sealed class InternalAccessAuditWriter(AppDbContext db) : IInternalAccessAuditWriter
{
    /// <inheritdoc/>
    public async Task RecordAsync(
        Guid requesterTenantId,
        Guid targetTenantId,
        string result,
        CancellationToken ct)
    {
        var entry = InternalAccessAuditEntry.Create(
            requesterTenantId,
            targetTenantId,
            result,
            DateTimeOffset.UtcNow);

        db.InternalAccessAuditLog.Add(entry);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
