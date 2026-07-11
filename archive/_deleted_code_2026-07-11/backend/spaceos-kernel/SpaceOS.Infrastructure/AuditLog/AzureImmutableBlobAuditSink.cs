// SpaceOS.Infrastructure/AuditLog/AzureImmutableBlobAuditSink.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Production stub for <see cref="IExternalAuditSink"/> targeting Azure Immutable Blob Storage.
/// Write and read operations are no-ops until the Azure Blob SDK append path is implemented.
/// </summary>
internal sealed class AzureImmutableBlobAuditSink : IExternalAuditSink
{
    private const string ConnectionStringKey = "Sink:AzureImmutableBlobConnectionString";

    private readonly ILogger<AzureImmutableBlobAuditSink> _logger;
    private readonly bool _isConfigured;

    /// <summary>
    /// Initialises a new <see cref="AzureImmutableBlobAuditSink"/>.
    /// </summary>
    /// <param name="configuration">Application configuration used to detect the connection string.</param>
    /// <param name="logger">Logger for configuration warnings.</param>
    public AzureImmutableBlobAuditSink(
        IConfiguration configuration,
        ILogger<AzureImmutableBlobAuditSink> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _logger       = logger;
        _isConfigured = !string.IsNullOrWhiteSpace(configuration[ConnectionStringKey]);

        if (!_isConfigured)
        {
            _logger.LogWarning(
                "Azure Immutable Blob Sink not fully configured. " +
                "Set '{Key}' in configuration to enable.", ConnectionStringKey);
        }
    }

    /// <inheritdoc/>
    public Task WriteAsync(
        Guid           tenantId,
        string         eventType,
        string         stateHash,
        string         previousHash,
        DateTimeOffset occurredAt,
        CancellationToken ct = default)
    {
        // Stub: replace with Azure.Storage.Blobs append-blob write when Key is configured.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        // Stub: Azure Blob read path not yet implemented.
        // Chain verification will report ExternalSinkMatch = false when this returns empty.
        return Task.FromResult<IReadOnlyList<ExternalAuditHashRecord>>(Array.Empty<ExternalAuditHashRecord>());
    }
}
