// SpaceOS.Infrastructure/AuditLog/NullAuditEscrowWriter.cs

using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// No-op implementation of <see cref="IAuditEscrowWriter"/> used in Development and test environments
/// where a MinIO bucket is not available.
/// </summary>
internal sealed class NullAuditEscrowWriter : IAuditEscrowWriter
{
    /// <inheritdoc/>
    /// <remarks>Always completes immediately without performing any I/O.</remarks>
    public Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default) =>
        Task.CompletedTask;
}
