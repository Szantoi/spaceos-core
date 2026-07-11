namespace SpaceOS.Modules.Joinery.Domain.Services;

/// <summary>
/// Service for building Gyártásilap (manufacturing spec sheet) PDFs.
/// Supports 4 label variants: L1 (basic), L2 (QR), L3 (barcode), L4 (full).
/// </summary>
public interface IGyartasilapPdfBuilder
{
    /// <summary>
    /// Generates a PDF for a Gyártásilap with the specified variant.
    /// </summary>
    /// <param name="orderNumber">Order reference number</param>
    /// <param name="orderName">Customer-facing project name</param>
    /// <param name="variant">Label variant: L1, L2, L3, or L4</param>
    /// <param name="materialList">Material line items (optional, for L2, L3, L4)</param>
    /// <param name="jobsList">Cutting/assembly jobs (optional, for L2, L3, L4)</param>
    /// <param name="notes">Assembly notes (optional, for L4)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF content as byte array</returns>
    Task<byte[]> GeneratePdfAsync(
        string orderNumber,
        string? orderName,
        string variant,
        IReadOnlyList<MaterialItem>? materialList = null,
        IReadOnlyList<JobItem>? jobsList = null,
        string? notes = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Material line item for PDF content.
/// </summary>
public record MaterialItem(
    string Code,
    string Description,
    decimal Quantity,
    string Unit);

/// <summary>
/// Cutting/assembly job for PDF content.
/// </summary>
public record JobItem(
    string Name,
    string Description);
