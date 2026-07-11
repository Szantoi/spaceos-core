// SpaceOS.Infrastructure/AuditLog/MinioAuditEscrowWriter.cs

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// WORM audit escrow writer that persists every committed <see cref="AuditEvent"/> to a
/// MinIO bucket configured with Object Lock COMPLIANCE mode (365 days).
///
/// <para>
/// <strong>Object key scheme:</strong>
/// <c>{tenantId}/{year}/{month:D2}/{eventId}.json</c>
/// Example: <c>79d71e39-5571-43d8-9f07-5f7631afa5e7/2026/04/d1a2b3c4-….json</c>
/// </para>
///
/// <para>
/// <strong>Metadata written per object:</strong>
/// <list type="bullet">
///   <item><c>x-amz-meta-audit-chain-hash</c> — the event's <see cref="AuditEvent.StateHash"/></item>
///   <item><c>x-amz-meta-created-at</c> — <see cref="AuditEvent.OccurredAt"/> in ISO 8601</item>
///   <item><c>x-amz-meta-tenant-id</c> — <see cref="AuditEvent.TenantId"/></item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Idempotency:</strong> Before writing, the object's existence is checked via
/// <c>StatObject</c>. If the object already exists the write is silently skipped, making
/// retries safe for at-least-once delivery scenarios.
/// </para>
///
/// <para>
/// <strong>Fire-and-forget safety:</strong> All exceptions are caught and logged with
/// <c>LogError</c>. A MinIO outage never blocks or rolls back the primary audit DB write.
/// </para>
/// </summary>
internal sealed class MinioAuditEscrowWriter : IAuditEscrowWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IMinioStorage _storage;
    private readonly MinioEscrowOptions _options;
    private readonly ILogger<MinioAuditEscrowWriter> _logger;

    /// <summary>Initialises a new <see cref="MinioAuditEscrowWriter"/>.</summary>
    /// <param name="storage">The MinIO storage adapter.</param>
    /// <param name="options">Escrow configuration (bucket name, enabled flag).</param>
    /// <param name="logger">Logger for error reporting.</param>
    public MinioAuditEscrowWriter(
        IMinioStorage storage,
        IOptions<MinioEscrowOptions> options,
        ILogger<MinioAuditEscrowWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _storage = storage;
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Skips the write when <see cref="MinioEscrowOptions.Enabled"/> is <see langword="false"/>.
    /// All exceptions from the MinIO call are caught and logged — never rethrown.
    /// </remarks>
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        var objectKey = BuildObjectKey(auditEvent);

        try
        {
            // Idempotency: skip if the object already exists (e.g. retry after partial failure).
            if (await _storage.ObjectExistsAsync(_options.BucketName, objectKey, ct).ConfigureAwait(false))
                return;

            var json  = JsonSerializer.Serialize(auditEvent, SerializerOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var stream = new MemoryStream(bytes, writable: false);

            await _storage.PutObjectAsync(
                bucket:      _options.BucketName,
                objectKey:   objectKey,
                content:     stream,
                size:        bytes.Length,
                contentType: "application/json",
                metadata:    BuildMetadata(auditEvent),
                ct:          ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Fire-and-forget contract: never rethrow — a MinIO outage must not block the primary write.
            _logger.LogError(ex,
                "WORM escrow write failed for audit event {EventId} tenant {TenantId}. " +
                "The event is persisted in the primary database; the escrow entry is missing.",
                auditEvent.Id,
                auditEvent.TenantId);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the MinIO object key: <c>{tenantId}/{year}/{month:D2}/{eventId}.json</c>.
    /// </summary>
    internal static string BuildObjectKey(AuditEvent auditEvent)
    {
        var year  = auditEvent.OccurredAt.Year;
        var month = auditEvent.OccurredAt.Month.ToString("D2");
        return $"{auditEvent.TenantId}/{year}/{month}/{auditEvent.Id}.json";
    }

    private static Dictionary<string, string> BuildMetadata(AuditEvent auditEvent) => new()
    {
        ["x-amz-meta-audit-chain-hash"] = auditEvent.StateHash,
        ["x-amz-meta-created-at"]       = auditEvent.OccurredAt.ToString("O"),
        ["x-amz-meta-tenant-id"]        = auditEvent.TenantId.ToString(),
    };
}
