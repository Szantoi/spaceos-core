// SpaceOS.Kernel.Application/AuditLog/IAuditEventDispatcher.cs

using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Converts domain events into <see cref="SpaceOS.Kernel.Domain.AuditLog.AuditEvent"/> records
/// and persists them via <see cref="SpaceOS.Kernel.Domain.AuditLog.IAuditEventRepository"/>.
/// </summary>
public interface IAuditEventDispatcher
{
    /// <summary>
    /// Serialises each event to canonical JSON, computes its SHA-256 hash, and persists the
    /// resulting audit entries.  An empty list is a no-op.
    /// </summary>
    /// <param name="events">The domain events to audit.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
}
