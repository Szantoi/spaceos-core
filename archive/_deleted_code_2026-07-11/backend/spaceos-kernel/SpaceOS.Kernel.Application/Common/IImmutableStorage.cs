// SpaceOS.Kernel.Application/Common/IImmutableStorage.cs

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Write-once immutable file storage abstraction.
/// Implementations must ensure that stored content is never modified or deleted after initial write.
/// </summary>
public interface IImmutableStorage
{
    /// <summary>
    /// Stores the content stream under the given file name and returns its SHA-256 hex hash.
    /// </summary>
    /// <param name="fileName">The logical file name (path) under which the content is stored.</param>
    /// <param name="content">The content stream to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The lowercase hex-encoded SHA-256 hash of the stored content.</returns>
    Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the stored content stream and verifies its integrity against the expected hash.
    /// </summary>
    /// <param name="fileName">The logical file name (path) of the stored content.</param>
    /// <param name="expectedHash">The expected lowercase hex-encoded SHA-256 hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readable stream of the verified content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the computed hash of the retrieved content does not match <paramref name="expectedHash"/>.
    /// </exception>
    Task<Stream> RetrieveAndVerifyAsync(string fileName, string expectedHash, CancellationToken ct = default);
}
