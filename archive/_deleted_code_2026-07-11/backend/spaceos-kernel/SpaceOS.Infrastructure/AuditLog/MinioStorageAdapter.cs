// SpaceOS.Infrastructure/AuditLog/MinioStorageAdapter.cs

using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Production implementation of <see cref="IMinioStorage"/> backed by the official MinIO .NET SDK.
/// </summary>
internal sealed class MinioStorageAdapter : IMinioStorage
{
    private readonly IMinioClient _client;

    /// <summary>Initialises a new <see cref="MinioStorageAdapter"/>.</summary>
    /// <param name="client">The configured MinIO client.</param>
    public MinioStorageAdapter(IMinioClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc/>
    public async Task PutObjectAsync(
        string bucket,
        string objectKey,
        Stream content,
        long size,
        string contentType,
        Dictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(size)
            .WithContentType(contentType)
            .WithHeaders(metadata);

        await _client.PutObjectAsync(args, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> ObjectExistsAsync(string bucket, string objectKey, CancellationToken ct = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey);

            await _client.StatObjectAsync(args, ct).ConfigureAwait(false);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }
}
