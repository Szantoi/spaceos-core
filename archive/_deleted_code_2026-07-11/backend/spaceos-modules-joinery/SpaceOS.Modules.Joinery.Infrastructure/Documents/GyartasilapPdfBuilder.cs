using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Documents;

/// <summary>
/// QuestPDF-based builder for Gyártásilap (manufacturing spec sheet) documents.
/// Generates 4 label variants with varying complexity and content.
/// </summary>
public sealed class GyartasilapPdfBuilder : IGyartasilapPdfBuilder
{
    static GyartasilapPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePdfAsync(
        string orderNumber,
        string? orderName,
        string variant,
        IReadOnlyList<MaterialItem>? materialList = null,
        IReadOnlyList<JobItem>? jobsList = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Validate variant
        if (!new[] { "L1", "L2", "L3", "L4" }.Contains(variant, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid variant '{variant}'. Must be L1, L2, L3, or L4.", nameof(variant));

        // QuestPDF is synchronous, so we wrap the generation in a Task
        return await Task.Run(() =>
        {
            var ms = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Item().Element(ComposeHeader(orderNumber, orderName, variant));
                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        switch (variant.ToUpperInvariant())
                        {
                            case "L1":
                                col.Item().Element(ComposeBasicLabel());
                                break;
                            case "L2":
                                col.Item().Element(ComposePremiumLabel(materialList ?? new List<MaterialItem>()));
                                break;
                            case "L3":
                                col.Item().Element(ComposeBarcodeLabel(orderNumber, materialList ?? new List<MaterialItem>()));
                                break;
                            case "L4":
                                col.Item().Element(ComposeFullLabel(orderNumber, materialList ?? new List<MaterialItem>(), jobsList ?? new List<JobItem>(), notes));
                                break;
                        }
                    });
                });
            }).GeneratePdf(ms);

            ms.Position = 0;
            return ms.ToArray();
        }, cancellationToken).ConfigureAwait(false);
    }

    private static Action<IContainer> ComposeHeader(string orderNumber, string? orderName, string variant) => container =>
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(inner =>
            {
                inner.Item().Text("GYÁRTÁSILAP").FontSize(18).Bold();
                inner.Item().PaddingTop(5).Text($"Order: {orderNumber}").FontSize(12);
                if (!string.IsNullOrWhiteSpace(orderName))
                    inner.Item().Text($"Project: {orderName}").FontSize(11);
            });

            row.RelativeItem().AlignRight().AlignMiddle().Text(variant).FontSize(20).Bold();
        });
    };

    private static Action<IContainer> ComposeBasicLabel() => container =>
    {
        container.Column(col =>
        {
            col.Item().Text("Standard Manufacturing Specification").FontSize(11).Bold();
            col.Item().PaddingTop(8).Text(
                "This document contains the manufacturing specifications for the referenced order. " +
                "Please follow all instructions and specifications carefully.").FontSize(10);
        });
    };

    private static Action<IContainer> ComposePremiumLabel(IReadOnlyList<MaterialItem> materialList) => container =>
    {
        container.Column(col =>
        {
            col.Item().Text("Premium Manufacturing Specification with QR Code").FontSize(11).Bold();

            if (materialList.Count > 0)
            {
                col.Item().PaddingTop(12).Element(ComposeMaterialsTable(materialList));
            }

            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("Order Details").FontSize(10).Bold();
                    inner.Item().PaddingTop(4).Text("QR Code below:").FontSize(9);
                });

                row.RelativeItem().AlignRight().Width(80).Height(80)
                    .Border(1)
                    .Padding(5)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("[QR Code]").FontSize(8);
            });
        });
    };

    private static Action<IContainer> ComposeBarcodeLabel(string orderNumber, IReadOnlyList<MaterialItem> materialList) => container =>
    {
        container.Column(col =>
        {
            col.Item().Text("Barcode Manufacturing Specification").FontSize(11).Bold();

            if (materialList.Count > 0)
            {
                col.Item().PaddingTop(12).Element(ComposeMaterialsTable(materialList));
            }

            col.Item().PaddingTop(12).Column(inner =>
            {
                inner.Item().Text("Item Barcode").FontSize(10).Bold();
                inner.Item().PaddingTop(4).Height(60).Border(1)
                    .Padding(5)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text($"Barcode: {orderNumber}").FontSize(9);
            });
        });
    };

    private static Action<IContainer> ComposeFullLabel(string orderNumber, IReadOnlyList<MaterialItem> materialList, IReadOnlyList<JobItem> jobsList, string? notes) => container =>
    {
        container.Column(col =>
        {
            col.Item().Text("Complete Manufacturing Specification").FontSize(11).Bold();

            if (materialList.Count > 0)
            {
                col.Item().PaddingTop(12).Element(ComposeMaterialsTable(materialList));
            }

            if (jobsList.Count > 0)
            {
                col.Item().PaddingTop(12).Element(ComposeJobsTable(jobsList));
            }

            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("Barcode").FontSize(10).Bold();
                    inner.Item().PaddingTop(4).Height(50).Border(1)
                        .Padding(5)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text($"{orderNumber}").FontSize(9);
                });

                row.RelativeItem().AlignRight().Column(inner =>
                {
                    inner.Item().Text("QR Code").FontSize(10).Bold();
                    inner.Item().PaddingTop(4).Width(80).Height(80).Border(1)
                        .Padding(5)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("[QR]").FontSize(8);
                });
            });

            if (!string.IsNullOrWhiteSpace(notes))
            {
                col.Item().PaddingTop(12).Column(inner =>
                {
                    inner.Item().Text("Assembly Instructions").FontSize(10).Bold();
                    inner.Item().PaddingTop(4).Text(notes).FontSize(9);
                });
            }
        });
    };

    private static Action<IContainer> ComposeMaterialsTable(IReadOnlyList<MaterialItem> materialList) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Code
                columns.RelativeColumn(3); // Description
                columns.RelativeColumn(1); // Qty
                columns.RelativeColumn(1); // Unit
            });

            table.Header(header =>
            {
                header.Cell().Padding(4)
                    .Text("Code").FontSize(9).Bold();
                header.Cell().Padding(4)
                    .Text("Description").FontSize(9).Bold();
                header.Cell().Padding(4)
                    .Text("Qty").FontSize(9).Bold();
                header.Cell().Padding(4)
                    .Text("Unit").FontSize(9).Bold();
            });

            foreach (var item in materialList)
            {
                table.Cell().Padding(3).Text(item.Code).FontSize(8);
                table.Cell().Padding(3).Text(item.Description).FontSize(8);
                table.Cell().Padding(3).Text(item.Quantity.ToString("N2")).FontSize(8);
                table.Cell().Padding(3).Text(item.Unit).FontSize(8);
            }
        });
    };

    private static Action<IContainer> ComposeJobsTable(IReadOnlyList<JobItem> jobsList) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Name
                columns.RelativeColumn(4); // Description
            });

            table.Header(header =>
            {
                header.Cell().Padding(4)
                    .Text("Task").FontSize(9).Bold();
                header.Cell().Padding(4)
                    .Text("Details").FontSize(9).Bold();
            });

            foreach (var job in jobsList)
            {
                table.Cell().Padding(3).Text(job.Name).FontSize(8);
                table.Cell().Padding(3).Text(job.Description).FontSize(8);
            }
        });
    };
}
