// SpaceOS.Kernel.Domain/Services/IProofStorageService.cs

namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Write-once, WORM-compatible proof file storage abstraction.
/// Implementations must ensure stored content is immutable after the initial write.
/// The <see cref="ProviderName"/> identifies the backend in stored metadata so that
/// future verification can locate the correct storage provider.
/// </summary>
public interface IProofStorageService
{
    /// <summary>
    /// Gets the short provider identifier stored alongside the proof hash
    /// (e.g. <c>"local"</c>, <c>"s3"</c>, <c>"azure"</c>).
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Uploads proof content and returns the SHA-256 hex hash and opaque storage key.
    /// </summary>
    /// <param name="content">The proof content stream. Must be readable and positioned at the start.</param>
    /// <param name="fileName">The logical file name. Path-traversal characters will be sanitised.</param>
    /// <param name="contentType">The MIME type of the content. Must be in the allowed list.</param>
    /// <param name="tenantId">The owning tenant identifier — used to scope the storage path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of <c>Hash</c> (lowercase SHA-256 hex) and <c>StorageKey</c> (opaque path
    /// that can be passed to <see cref="VerifyHashAsync"/>).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="contentType"/> is not allowed.</exception>
    Task<(string Hash, string StorageKey)> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid tenantId,
        CancellationToken ct);

    /// <summary>
    /// Verifies that the content identified by <paramref name="storageKey"/> matches
    /// <paramref name="expectedHash"/>.
    /// </summary>
    /// <param name="storageKey">The storage key returned by a previous <see cref="UploadAsync"/> call.</param>
    /// <param name="expectedHash">The expected lowercase SHA-256 hex hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> if the hash matches; <see langword="false"/> otherwise.</returns>
    Task<bool> VerifyHashAsync(string storageKey, string expectedHash, CancellationToken ct);

    /// <summary>
    /// Checks whether the storage backend is reachable.
    /// Must not throw — returns <see langword="false"/> on connectivity failure.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> if the backend is available; <see langword="false"/> otherwise.</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct);
}
