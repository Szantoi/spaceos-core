using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Cached reference to a generated production sheet PDF for a cutting list snapshot.
/// One snapshot can have at most one cache entry (enforced via UNIQUE FK in the database).
/// </summary>
public sealed class ProductionSheetCache : TenantScopedEntity
{
    public Guid SnapshotId { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; private set; }

    private ProductionSheetCache() { } // EF Core

    /// <summary>
    /// Creates a new production sheet cache entry.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="snapshotId">The snapshot this PDF was generated from.</param>
    /// <param name="filePath">Storage path to the PDF file (no path traversal segments allowed).</param>
    /// <param name="fileHash">SHA-256 or equivalent hash of the file content.</param>
    /// <param name="generatedAt">UTC timestamp when the file was generated.</param>
    public static ProductionSheetCache Create(
        Guid tenantId,
        Guid snapshotId,
        string filePath,
        string fileHash,
        DateTimeOffset generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);

        if (filePath.Contains(".."))
            throw new ArgumentException("FilePath must not contain path traversal segments.", nameof(filePath));

        return new ProductionSheetCache
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SnapshotId = snapshotId,
            FilePath = filePath,
            FileHash = fileHash,
            GeneratedAt = generatedAt
        };
    }
}
