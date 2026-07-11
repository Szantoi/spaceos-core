using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Core;

/// <summary>
/// Aggregate representing a generated material requirements list (anyaglista) PDF for a door order.
/// Stores a reference to the PDF in MinIO and optional byte content for small PDFs.
/// </summary>
public sealed class Anyaglista : TenantScopedEntity
{
    public Guid OrderId { get; private set; }
    public byte[]? PdfContent { get; private set; }
    public string? StorageUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Anyaglista() { } // EF Core

    /// <summary>Creates a new Anyaglista draft for the given order and tenant.</summary>
    public static Anyaglista Create(Guid orderId, Guid tenantId)
    {
        return new Anyaglista
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Sets the generated PDF content and storage URL.</summary>
    public void SetPdf(byte[] pdfContent, string storageUrl)
    {
        ArgumentNullException.ThrowIfNull(pdfContent);
        if (string.IsNullOrWhiteSpace(storageUrl))
            throw new ArgumentException("Storage URL cannot be empty.", nameof(storageUrl));

        PdfContent = pdfContent;
        StorageUrl = storageUrl;
    }
}
