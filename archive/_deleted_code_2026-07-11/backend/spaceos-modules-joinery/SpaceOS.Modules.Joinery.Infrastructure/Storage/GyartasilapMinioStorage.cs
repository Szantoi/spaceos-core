using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Storage;

/// <summary>
/// MinIO-backed PDF storage for Gyártásilap documents.
/// Stores PDFs in a WORM bucket at:
///   gyartasilap/{tenantId}/{planId}/gyartasilap_{variant}.pdf
/// </summary>
public sealed class GyartasilapMinioStorage : IGyartasilapStorage
{
    private readonly IMinioClient _client;
    private readonly GyartasilapStorageOptions _options;
    private readonly ILogger<GyartasilapMinioStorage> _logger;

    public GyartasilapMinioStorage(
        IMinioClient client,
        IOptions<GyartasilapStorageOptions> options,
        ILogger<GyartasilapMinioStorage> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> StoreAsync(
        Guid tenantId,
        Guid planId,
        string variant,
        byte[] pdfBytes,
        CancellationToken ct = default)
    {
        var objectKey = BuildObjectKey(tenantId, planId, variant);

        using var stream = new MemoryStream(pdfBytes, writable: false);

        var args = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(pdfBytes.Length)
            .WithContentType("application/pdf")
            .WithHeaders(new Dictionary<string, string>
            {
                ["x-amz-meta-tenant-id"] = tenantId.ToString(),
                ["x-amz-meta-label-variant"] = variant,
                ["x-amz-meta-generated-at"] = DateTimeOffset.UtcNow.ToString("O"),
            });

        await _client.PutObjectAsync(args, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Gyártásilap PDF stored in MinIO: bucket={Bucket} key={Key} size={Size}B",
            _options.BucketName, objectKey, pdfBytes.Length);

        return objectKey;
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectKey);

            await _client.StatObjectAsync(args, ct).ConfigureAwait(false);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    public async Task<string> StoreZipAsync(
        Guid tenantId,
        Guid batchId,
        byte[] zipBytes,
        CancellationToken ct = default)
    {
        var objectKey = $"{tenantId}/batches/{batchId}/batch.zip";

        using var stream = new MemoryStream(zipBytes, writable: false);

        var args = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(zipBytes.Length)
            .WithContentType("application/zip")
            .WithHeaders(new Dictionary<string, string>
            {
                ["x-amz-meta-tenant-id"] = tenantId.ToString(),
                ["x-amz-meta-batch-id"] = batchId.ToString(),
                ["x-amz-meta-generated-at"] = DateTimeOffset.UtcNow.ToString("O"),
            });

        await _client.PutObjectAsync(args, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Batch ZIP stored in MinIO: bucket={Bucket} key={Key} size={Size}B",
            _options.BucketName, objectKey, zipBytes.Length);

        return objectKey;
    }

    public async Task<string> GetPresignedUrlAsync(string storagePath, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storagePath)
            .WithExpiry(3600); // 1 hour

        var url = await _client.PresignedGetObjectAsync(args).ConfigureAwait(false);

        return ReplaceHost(url, _options.PublicEndpoint);
    }

    internal static string ReplaceHost(string presignedUrl, string? publicEndpoint)
    {
        if (string.IsNullOrWhiteSpace(publicEndpoint))
            return presignedUrl;

        var uri = new Uri(presignedUrl);
        var publicBase = publicEndpoint.TrimEnd('/');

        return $"{publicBase}{uri.PathAndQuery}";
    }

    public async Task<string> StoreAnyaglistaPdfAsync(
        Guid tenantId,
        Guid orderId,
        byte[] pdfBytes,
        CancellationToken ct = default)
    {
        var objectKey = $"anyaglista/{tenantId}/{orderId}/anyaglista.pdf";

        using var stream = new MemoryStream(pdfBytes, writable: false);

        var args = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(pdfBytes.Length)
            .WithContentType("application/pdf")
            .WithHeaders(new Dictionary<string, string>
            {
                ["x-amz-meta-tenant-id"] = tenantId.ToString(),
                ["x-amz-meta-order-id"] = orderId.ToString(),
                ["x-amz-meta-generated-at"] = DateTimeOffset.UtcNow.ToString("O"),
            });

        await _client.PutObjectAsync(args, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Anyaglista PDF stored in MinIO: bucket={Bucket} key={Key} size={Size}B",
            _options.BucketName, objectKey, pdfBytes.Length);

        return objectKey;
    }

    /// <summary>Key scheme: gyartasilap/{tenantId}/{planId}/gyartasilap_{variant}.pdf</summary>
    public static string BuildObjectKey(Guid tenantId, Guid planId, string variant) =>
        $"{tenantId}/{planId}/gyartasilap_{variant}.pdf";
}
