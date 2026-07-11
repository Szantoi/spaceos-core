// SpaceOS.Infrastructure/Storage/LocalProofStorageService.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Infrastructure.Storage;

/// <summary>
/// Development implementation of <see cref="IProofStorageService"/> that stores proof files
/// on the local file system under <c>uploads/proofs/</c>.
/// Not suitable for production — use <see cref="S3WormProofStorageService"/> instead.
/// </summary>
/// <remarks>
/// Storage key format: <c>{tenantId}/{yyyy/MM/dd}/{guid}_{sanitizedFileName}</c> (SEC-P3B-01).
/// </remarks>
internal sealed class LocalProofStorageService : IProofStorageService
{
    private const string RootDirectory = "uploads/proofs";

    /// <summary>Allowed MIME types for proof uploads (SEC-P3B-03 whitelist).</summary>
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf",
        "video/mp4",
        "video/webm"
    };

    private readonly ILogger<LocalProofStorageService> _logger;

    /// <summary>Initialises a new <see cref="LocalProofStorageService"/>.</summary>
    /// <param name="logger">Structured logger.</param>
    public LocalProofStorageService(ILogger<LocalProofStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public string ProviderName => "local";

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="contentType"/> is not in the allowed MIME type whitelist.
    /// </exception>
    public async Task<(string Hash, string StorageKey)> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid tenantId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (!AllowedMimeTypes.Contains(contentType))
            throw new InvalidOperationException(
                $"Content type '{contentType}' is not permitted. " +
                $"Allowed types: {string.Join(", ", AllowedMimeTypes)}.");

        var today      = DateTimeOffset.UtcNow;
        var datePath   = today.ToString("yyyy/MM/dd");
        var fileId     = Guid.NewGuid().ToString("N");
        var sanitized  = SanitizeFileName(fileName);
        var storageKey = $"{tenantId:D}/{datePath}/{fileId}_{sanitized}";

        var directory = Path.GetFullPath(Path.Combine(RootDirectory, tenantId.ToString("D"), datePath));
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{fileId}_{sanitized}");

        // Buffer content so we can compute hash before writing
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct).ConfigureAwait(false);
        buffer.Position = 0;

        var hash = ComputeSha256Hex(buffer.ToArray());
        buffer.Position = 0;

        await using var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await buffer.CopyToAsync(fileStream, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Stored proof file {StorageKey} for tenant {TenantId} (hash: {Hash}).",
            storageKey, tenantId, hash);

        return (hash, storageKey);
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyHashAsync(string storageKey, string expectedHash, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);

        var filePath = Path.GetFullPath(Path.Combine(RootDirectory, storageKey));

        if (!File.Exists(filePath))
            return false;

        var bytes      = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
        var actualHash = ComputeSha256Hex(bytes);

        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(true);

    private static string ComputeSha256Hex(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Removes path-traversal characters and illegal file name characters (SEC-P3B-01).
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Strip directory separators first (prevents path traversal)
        fileName = fileName.Replace("..", string.Empty, StringComparison.Ordinal);
        fileName = fileName.Replace('/', '_').Replace('\\', '_');

        var invalid   = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(fileName.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));

        // Truncate to a reasonable length to avoid OS path limits
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }
}
