// SpaceOS.Infrastructure/AuditLog/IMinioStorage.cs

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Thin adapter over the MinIO client for the audit escrow use case.
/// Defined in Infrastructure (not Application) because it is a pure infrastructure
/// concern — no domain type crosses the boundary.
/// Extracted for unit-test mockability without depending on the full <c>IMinioClient</c> interface.
/// </summary>
internal interface IMinioStorage
{
    /// <summary>
    /// Uploads an object to the specified bucket.
    /// </summary>
    /// <param name="bucket">Target bucket name.</param>
    /// <param name="objectKey">Full object key (path within the bucket).</param>
    /// <param name="content">Content stream positioned at offset 0.</param>
    /// <param name="size">Exact byte length of <paramref name="content"/>.</param>
    /// <param name="contentType">MIME type of the object.</param>
    /// <param name="metadata">User-defined metadata headers (<c>x-amz-meta-*</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    Task PutObjectAsync(
        string bucket,
        string objectKey,
        Stream content,
        long size,
        string contentType,
        Dictionary<string, string> metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Returns <see langword="true"/> when the object exists; <see langword="false"/> when it does not.
    /// Must not throw for 404 — only for connectivity failures.
    /// </summary>
    /// <param name="bucket">Target bucket name.</param>
    /// <param name="objectKey">Full object key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ObjectExistsAsync(string bucket, string objectKey, CancellationToken ct = default);
}
