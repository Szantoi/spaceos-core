// SpaceOS.Infrastructure/AuditLog/FileExternalAuditSink.cs
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Development-only implementation of <see cref="IExternalAuditSink"/> that appends
/// hash records to <c>logs/audit-hashes.log</c> relative to the working directory.
/// Thread-safe via a <see cref="SemaphoreSlim"/>. I/O errors are swallowed so a
/// disk or permission failure never blocks the primary audit write path.
/// </summary>
/// <remarks>
/// Each line written to the log has the format:
/// <c>{occurredAt:O}|{tenantId}|{eventType}|{previousHash}→{stateHash}</c>
/// </remarks>
internal sealed class FileExternalAuditSink : IExternalAuditSink
{
    private static readonly string LogPath =
        Path.Combine("logs", "audit-hashes.log");

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<FileExternalAuditSink> _logger;

    /// <summary>
    /// Initialises a new <see cref="FileExternalAuditSink"/>.
    /// </summary>
    /// <param name="logger">Logger used to report I/O errors swallowed by the sink.</param>
    public FileExternalAuditSink(ILogger<FileExternalAuditSink> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(
        Guid           tenantId,
        string         eventType,
        string         stateHash,
        string         previousHash,
        DateTimeOffset occurredAt,
        CancellationToken ct = default)
    {
        var line = $"{occurredAt:O}|{tenantId}|{eventType}|{previousHash}→{stateHash}\n";

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            EnsureDirectoryExists();
            await File.AppendAllTextAsync(LogPath, line, ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "FileExternalAuditSink: failed to write audit hash record.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        if (!File.Exists(LogPath))
            return Array.Empty<ExternalAuditHashRecord>();

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var lines = await File.ReadAllLinesAsync(LogPath, ct).ConfigureAwait(false);
            var results = new List<ExternalAuditHashRecord>();

            foreach (var rawLine in lines)
            {
                var record = TryParseLine(rawLine);
                if (record is null)
                    continue;

                if (record.TenantId != tenantId)
                    continue;

                if (from.HasValue && record.OccurredAt < from.Value)
                    continue;

                if (to.HasValue && record.OccurredAt > to.Value)
                    continue;

                results.Add(record);
            }

            // Already written in OccurredAt order; sort defensively in case of concurrent writes.
            results.Sort(static (a, b) => a.OccurredAt.CompareTo(b.OccurredAt));
            return results.AsReadOnly();
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "FileExternalAuditSink: failed to read audit hash records.");
            return Array.Empty<ExternalAuditHashRecord>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(LogPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>
    /// Parses a single log line in the format:
    /// <c>{occurredAt:O}|{tenantId}|{eventType}|{previousHash}→{stateHash}</c>
    /// Returns <see langword="null"/> if the line is malformed.
    /// </summary>
    private static ExternalAuditHashRecord? TryParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var parts = line.Split('|');
        if (parts.Length != 4)
            return null;

        if (!DateTimeOffset.TryParse(parts[0], out var occurredAt))
            return null;

        if (!Guid.TryParse(parts[1], out var tenantId))
            return null;

        var eventType = parts[2];

        // parts[3] = "{previousHash}→{stateHash}"
        var arrowIdx = parts[3].IndexOf('→', StringComparison.Ordinal);
        if (arrowIdx < 0)
            return null;

        var previousHash = parts[3][..arrowIdx];
        var stateHash    = parts[3][(arrowIdx + "→".Length)..].TrimEnd('\n', '\r');

        return new ExternalAuditHashRecord(occurredAt, tenantId, eventType, previousHash, stateHash);
    }
}
