namespace SpaceOS.Modules.Joinery.Domain.Services;

/// <summary>
/// Storage service for Gyártásilap PDFs. Targets a MinIO WORM bucket.
/// Falls back to BYTEA-in-DB when MinIO is unavailable.
/// Key scheme: gyartasilap/{tenantId}/{planId}/gyartasilap_{variant}.pdf
/// </summary>
public interface IGyartasilapStorage
{
    /// <summary>
    /// Uploads PDF bytes to MinIO and returns the object key (StorageUrl).
    /// </summary>
    Task<string> StoreAsync(
        Guid tenantId,
        Guid planId,
        string variant,
        byte[] pdfBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether the specified object already exists (idempotency guard).
    /// </summary>
    Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Uploads a ZIP archive containing multiple Gyártásilap PDFs.
    /// Returns the object key (storage path).
    /// </summary>
    Task<string> StoreZipAsync(
        Guid tenantId,
        Guid batchId,
        byte[] zipBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a pre-signed URL for temporary direct download of the given object.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string storagePath, CancellationToken ct = default);

    /// <summary>
    /// Uploads an Anyaglista PDF to MinIO.
    /// Returns the object key (storage path).
    /// Key scheme: anyaglista/{tenantId}/{orderId}/anyaglista.pdf
    /// </summary>
    Task<string> StoreAnyaglistaPdfAsync(
        Guid tenantId,
        Guid orderId,
        byte[] pdfBytes,
        CancellationToken ct = default);
}
