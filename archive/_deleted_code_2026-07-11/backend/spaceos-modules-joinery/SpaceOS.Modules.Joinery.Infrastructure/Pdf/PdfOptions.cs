namespace SpaceOS.Modules.Joinery.Infrastructure.Pdf;

/// <summary>
/// Configuration options for PDF file storage.
/// Bound from the <c>Pdf</c> configuration section.
/// </summary>
public sealed class PdfOptions
{
    /// <summary>
    /// Root directory under which tenant-scoped PDF files are stored.
    /// Defaults to <c>/opt/spaceos/data/joinery/pdf</c>.
    /// </summary>
    public string BasePath { get; set; } = "/opt/spaceos/data/joinery/pdf";
}
