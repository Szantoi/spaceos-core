using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Documents;

/// <summary>
/// QuestPDF-based builder for Anyaglista (material requirements list) documents.
/// Generates a tabular PDF with header, material rows, and a footer summary.
/// </summary>
public sealed class AnyaglistaPdfBuilder : IAnyaglistaPdfBuilder
{
    static AnyaglistaPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public byte[] GeneratePdf(AnyaglistaData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().Element(ComposeHeader(data));
                    col.Item().PaddingVertical(10).LineHorizontal(1);
                    col.Item().Element(ComposeTable(data.Rows));
                    col.Item().PaddingTop(10).Element(ComposeFooter(data.Rows.Count));
                });
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms.ToArray();
    }

    private static Action<IContainer> ComposeHeader(AnyaglistaData data) => container =>
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(inner =>
            {
                inner.Item().Text("ANYAGLISTA").FontSize(18).Bold();
                inner.Item().PaddingTop(4)
                    .Text($"Rendelés: {data.OrderId}").FontSize(11);
                inner.Item().Text($"Ügyfél: {data.CustomerName}").FontSize(11);
                inner.Item().Text(
                    $"Generálva: {data.GeneratedAt:yyyy-MM-dd HH:mm}").FontSize(9);
            });
        });
    };

    private static Action<IContainer> ComposeTable(IReadOnlyList<AnyaglistaRow> rows) =>
        container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // MaterialType
                columns.RelativeColumn(2); // SupplierCode
                columns.RelativeColumn(1); // Quantity
                columns.RelativeColumn(1); // Unit
                columns.RelativeColumn(2); // Notes
            });

            // Header row
            table.Header(header =>
            {
                header.Cell().Padding(4).Text("Anyagtípus").FontSize(9).Bold();
                header.Cell().Padding(4).Text("Szállítói kód").FontSize(9).Bold();
                header.Cell().Padding(4).Text("Mennyiség").FontSize(9).Bold();
                header.Cell().Padding(4).Text("Egység").FontSize(9).Bold();
                header.Cell().Padding(4).Text("Megjegyzés").FontSize(9).Bold();
            });

            foreach (var row in rows)
            {
                table.Cell().Padding(3).Text(row.MaterialType).FontSize(8);
                table.Cell().Padding(3).Text(row.SupplierCode).FontSize(8);
                table.Cell().Padding(3).Text(row.Quantity.ToString("N2")).FontSize(8);
                table.Cell().Padding(3).Text(row.Unit).FontSize(8);
                table.Cell().Padding(3).Text(row.Notes ?? string.Empty).FontSize(8);
            }
        });
    };

    private static Action<IContainer> ComposeFooter(int rowCount) => container =>
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Sorok száma összesen: {rowCount}").FontSize(9).Bold();
        });
    };
}
