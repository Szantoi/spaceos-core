// SpaceOS.Infrastructure/Storage/FileImmutableStorage.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Infrastructure.Storage;

/// <summary>
/// Development implementation of <see cref="IImmutableStorage"/> that writes files to the local file system
/// under <c>uploads/immutable/</c> relative to the working directory.
/// Not suitable for production — use <see cref="AzureImmutableBlobStorage"/> instead.
/// </summary>
internal sealed class FileImmutableStorage : IImmutableStorage
{
    private const string RootDirectory = "uploads/immutable";

    private readonly ILogger<FileImmutableStorage> _logger;

    /// <summary>
    /// Initialises a new <see cref="FileImmutableStorage"/>.
    /// </summary>
    /// <param name="logger">Structured logger.</param>
    public FileImmutableStorage(ILogger<FileImmutableStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        var directory = Path.GetFullPath(RootDirectory);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, SanitiseFileName(fileName));

        // Buffer content so we can compute the hash after writing
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct).ConfigureAwait(false);
        buffer.Position = 0;

        var hash = ComputeSha256Hex(buffer.ToArray());
        buffer.Position = 0;

        await using var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await buffer.CopyToAsync(fileStream, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Stored immutable file {FileName} → {FilePath} (hash: {Hash}).",
            fileName, filePath, hash);

        return hash;
    }

    /// <inheritdoc/>
    public async Task<Stream> RetrieveAndVerifyAsync(string fileName, string expectedHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);

        var directory = Path.GetFullPath(RootDirectory);
        var filePath = Path.Combine(directory, SanitiseFileName(fileName));

        var bytes = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
        var actualHash = ComputeSha256Hex(bytes);

        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Hash mismatch for file '{fileName}': expected '{expectedHash}', got '{actualHash}'.");
        }

        return new MemoryStream(bytes);
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string SanitiseFileName(string fileName)
    {
        // Replace path separators and illegal characters with underscores
        var invalid = Path.GetInvalidFileNameChars();
        var sanitised = string.Concat(fileName.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
        return sanitised;
    }
}
