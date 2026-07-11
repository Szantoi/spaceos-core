// SpaceOS.Infrastructure/Storage/S3WormProofStorageService.cs

using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Infrastructure.Storage;

/// <summary>
/// Production stub for WORM-compatible proof file storage backed by Amazon S3 Object Lock
/// in GOVERNANCE mode. Full S3 SDK integration is provisioned separately via infrastructure
/// scripts when the AWS SDK package is approved.
/// </summary>
/// <remarks>
/// <para>
/// This stub validates the interface contract and logs all calls so that integration tests
/// can confirm that the correct provider is selected in non-development environments.
/// </para>
/// <para>
/// Replace the body of <see cref="UploadAsync"/>, <see cref="VerifyHashAsync"/>, and
/// <see cref="IsAvailableAsync"/> with AWSSDK.S3 calls once the package is approved.
/// </para>
/// </remarks>
internal sealed class S3WormProofStorageService : IProofStorageService
{
    private const string BucketConfigKey = "Storage:S3:WormBucket";

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

    private readonly IConfiguration _configuration;
    private readonly ILogger<S3WormProofStorageService> _logger;

    /// <summary>Initialises a new <see cref="S3WormProofStorageService"/>.</summary>
    /// <param name="configuration">The application configuration (reads S3 bucket name).</param>
    /// <param name="logger">Structured logger.</param>
    public S3WormProofStorageService(
        IConfiguration configuration,
        ILogger<S3WormProofStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _configuration = configuration;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public string ProviderName => "s3";

    /// <inheritdoc/>
    /// <remarks>
    /// STUB — full S3 Object Lock upload not yet implemented.
    /// Computes the SHA-256 hash from the stream and logs the intent.
    /// </remarks>
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
                $"Content type '{contentType}' is not permitted for proof uploads.");

        var bucket = _configuration[BucketConfigKey]
            ?? throw new InvalidOperationException(
                $"S3 WORM bucket is not configured. Set '{BucketConfigKey}' in configuration.");

        // Compute hash from stream (stub — not uploaded to S3 yet)
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();

        var today      = DateTimeOffset.UtcNow;
        var storageKey = $"s3://{bucket}/{tenantId:D}/{today:yyyy/MM/dd}/{Guid.NewGuid():N}_{fileName}";

        _logger.LogWarning(
            "S3WormProofStorageService.UploadAsync is a stub. Would upload {StorageKey} to bucket {Bucket}.",
            storageKey, bucket);

        return (hash, storageKey);
    }

    /// <inheritdoc/>
    /// <remarks>STUB — returns <see langword="false"/> until S3 SDK integration is complete.</remarks>
    public Task<bool> VerifyHashAsync(string storageKey, string expectedHash, CancellationToken ct)
    {
        _logger.LogWarning(
            "S3WormProofStorageService.VerifyHashAsync is a stub. Returning false for key {StorageKey}.",
            storageKey);

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// STUB — performs no actual connectivity check.
    /// Returns <see langword="false"/> so that <c>VerifyChain</c> reports
    /// <c>WormStorageAvailable: false</c> without throwing (SEC-P3B-05).
    /// </remarks>
    public Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        _logger.LogWarning("S3WormProofStorageService.IsAvailableAsync is a stub — returning false.");
        return Task.FromResult(false);
    }
}
