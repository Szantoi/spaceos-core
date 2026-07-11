namespace SpaceOS.Modules.Joinery.Domain.Services;

/// <summary>
/// Builds a material requirements list (anyaglista) PDF from structured data.
/// Implemented in Infrastructure using QuestPDF.
/// </summary>
public interface IAnyaglistaPdfBuilder
{
    /// <summary>Generates the PDF bytes synchronously.</summary>
    byte[] GeneratePdf(AnyaglistaData data);
}

/// <summary>Data input for Anyaglista PDF generation.</summary>
public record AnyaglistaData(
    Guid OrderId,
    string CustomerName,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<AnyaglistaRow> Rows);

/// <summary>A single row in the material requirements table.</summary>
public record AnyaglistaRow(
    string MaterialType,
    string SupplierCode,
    decimal Quantity,
    string Unit,
    string? Notes);
