// SpaceOS.Infrastructure/Storage/AzureImmutableBlobStorage.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Storage;

/// <summary>
/// Production stub for Azure Immutable Blob Storage.
/// Full Azure SDK integration is deferred to Sprint 3.
/// <para>
/// NOTE (Sprint 3): Replace this stub with a real Azure Blob Storage client using
/// immutable storage policies (WORM — Write Once Read Many). Redis-backed deduplication
/// and SAS token generation will also be wired here.
/// </para>
/// </summary>
internal sealed class AzureImmutableBlobStorage : IImmutableStorage
{
    private readonly ILogger<AzureImmutableBlobStorage> _logger;

    /// <summary>
    /// Initialises a new <see cref="AzureImmutableBlobStorage"/>.
    /// </summary>
    /// <param name="logger">Structured logger.</param>
    public AzureImmutableBlobStorage(ILogger<AzureImmutableBlobStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Sprint 3 stub: computes and returns the SHA-256 hash without persisting content.
    /// </remarks>
    public async Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        _logger.LogWarning(
            "Azure Immutable Blob Storage not configured — Sprint 3. File '{FileName}' will not be persisted.",
            fileName);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct).ConfigureAwait(false);

        var hashBytes = SHA256.HashData(buffer.ToArray());
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always thrown — Azure Immutable Blob Storage is not yet configured.</exception>
    public Task<Stream> RetrieveAndVerifyAsync(string fileName, string expectedHash, CancellationToken ct = default)
    {
        throw new NotSupportedException("Azure Immutable Blob Storage not configured — Sprint 3.");
    }
}
