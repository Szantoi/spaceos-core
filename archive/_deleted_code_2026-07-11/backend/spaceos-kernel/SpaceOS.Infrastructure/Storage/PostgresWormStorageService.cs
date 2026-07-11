// SpaceOS.Infrastructure/Storage/PostgresWormStorageService.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Infrastructure.Storage;

/// <summary>
/// PostgreSQL WORM (Write-Once, Read-Many) implementation of <see cref="IWormStorageService"/>.
/// Connects using the <c>spaceos_audit_worm</c> role which has INSERT-only rights on
/// the <c>AuditHashes</c> table (SEC-03).
/// </summary>
/// <remarks>
/// <para>
/// <strong>SEC-07:</strong> The connection string is read from the <c>AUDIT_SINK_CONNECTION_STRING</c>
/// environment variable only — never from <c>appsettings.json</c>.
/// </para>
/// <para>
/// <strong>SEC-03:</strong> The role used by this connection string must have ONLY INSERT on
/// <c>AuditHashes</c>. SELECT, UPDATE and DELETE must be revoked.
/// </para>
/// </remarks>
internal sealed class PostgresWormStorageService : IWormStorageService
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresWormStorageService> _logger;

    /// <summary>
    /// Initialises a new <see cref="PostgresWormStorageService"/>.
    /// </summary>
    /// <param name="configuration">The application configuration. Reads <c>AUDIT_SINK_CONNECTION_STRING</c>.</param>
    /// <param name="logger">Structured logger.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AUDIT_SINK_CONNECTION_STRING</c> is not set.
    /// </exception>
    public PostgresWormStorageService(
        IConfiguration configuration,
        ILogger<PostgresWormStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionString = configuration["AUDIT_SINK_CONNECTION_STRING"]
            ?? throw new InvalidOperationException(
                "AUDIT_SINK_CONNECTION_STRING environment variable is not set. " +
                "This variable must be provided at runtime — do not add it to appsettings.json (SEC-07).");

        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Opens a short-lived <see cref="NpgsqlConnection"/> using the WORM role connection string
    /// and executes a single INSERT. The connection is disposed immediately after the command.
    /// Duplicate block indices are silently ignored via <c>ON CONFLICT DO NOTHING</c> —
    /// the WORM guarantee is "at-least-once write" per block.
    /// </remarks>
    public async Task AppendAsync(Guid tenantId, long blockIndex, string hash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO "AuditHashes" ("TenantId", "BlockIndex", "Hash")
            VALUES (@tenantId, @blockIndex, @hash)
            ON CONFLICT ("TenantId", "BlockIndex") DO NOTHING
            """;

        command.Parameters.AddWithValue("tenantId",   tenantId);
        command.Parameters.AddWithValue("blockIndex",  blockIndex);
        command.Parameters.AddWithValue("hash",        hash);

        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        _logger.LogDebug(
            "PostgresWormStorageService: appended block {BlockIndex} for tenant {TenantId}.",
            blockIndex, tenantId);
    }
}
