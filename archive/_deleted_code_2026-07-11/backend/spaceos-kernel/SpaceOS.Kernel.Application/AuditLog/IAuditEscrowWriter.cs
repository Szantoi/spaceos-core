// SpaceOS.Kernel.Application/AuditLog/IAuditEscrowWriter.cs

using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Write-once external escrow sink for <see cref="AuditEvent"/> records.
/// Implementations persist every audit event to a WORM storage backend so that
/// the event chain survives primary-database failures.
///
/// <para>
/// <strong>Contract:</strong> Implementations MUST be idempotent (writing the same
/// event twice is a no-op) and MUST NOT throw — failures are expected to be swallowed
/// and logged so that a sink outage never blocks the primary database write.
/// </para>
/// </summary>
public interface IAuditEscrowWriter
{
    /// <summary>
    /// Writes <paramref name="auditEvent"/> to the WORM escrow backend.
    /// If the event already exists, the call is a no-op.
    /// If the backend is unavailable, the exception is swallowed and logged.
    /// </summary>
    /// <param name="auditEvent">The committed audit event to escrow.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default);
}
