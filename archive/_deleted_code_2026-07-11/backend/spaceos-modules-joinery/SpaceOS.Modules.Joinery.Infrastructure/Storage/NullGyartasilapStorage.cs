using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Storage;

/// <summary>
/// No-op storage used when MinIO is not configured.
/// The handler falls back to DB BYTEA storage on exception.
/// </summary>
internal sealed class NullGyartasilapStorage : IGyartasilapStorage
{
    public Task<string> StoreAsync(Guid tenantId, Guid planId, string variant, byte[] pdfBytes, CancellationToken ct = default)
        => throw new InvalidOperationException("Gyártásilap MinIO storage is not configured.");

    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<string> StoreZipAsync(Guid tenantId, Guid batchId, byte[] zipBytes, CancellationToken ct = default)
        => throw new InvalidOperationException("Gyártásilap MinIO storage is not configured.");

    public Task<string> GetPresignedUrlAsync(string storagePath, CancellationToken ct = default)
        => throw new InvalidOperationException("Gyártásilap MinIO storage is not configured.");

    public Task<string> StoreAnyaglistaPdfAsync(Guid tenantId, Guid orderId, byte[] pdfBytes, CancellationToken ct = default)
        => throw new InvalidOperationException("Gyártásilap MinIO storage is not configured.");
}
