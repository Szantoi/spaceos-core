using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Infrastructure.Pdf;

/// <summary>
/// Generates a QuestPDF Community edition production sheet PDF for a door order.
/// SEC-08: ComponentName is truncated to 100 characters in all table rows.
/// </summary>
public sealed class ProductionSheetGenerator : IProductionSheetGenerator
{
    private const int MaxComponentNameLength = 100;

    static ProductionSheetGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    public Stream Generate(DoorOrder order, IReadOnlyList<CuttingListSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(snapshots);

        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader(order));
                page.Content().Element(ComposeBody(order, snapshots));
                page.Footer().AlignCenter().Text(
                    $"SpaceOS — Gyártásilap — {DateTime.UtcNow:yyyy-MM-dd}");
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    private static Action<IContainer> ComposeHeader(DoorOrder order) => container =>
    {
        var shortId = order.Id.ToString("N")[..8].ToUpperInvariant();
        var customerName = order.ProjectInfo?.ClientName ?? "—";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        container.Column(col =>
        {
            col.Item().Text("SpaceOS Gyártásilap")
                .FontSize(16).Bold().AlignCenter();

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Rendelés: {shortId}");
                row.RelativeItem().AlignCenter().Text($"Vevő: {customerName}");
                row.RelativeItem().AlignRight().Text($"Dátum: {date}");
            });

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    };

    private static Action<IContainer> ComposeBody(
        DoorOrder order,
        IReadOnlyList<CuttingListSnapshot> snapshots) => container =>
    {
        container.Column(col =>
        {
            col.Spacing(12);

            // Cutting list table
            var allLines = snapshots.SelectMany(s => s.Lines).ToList();
            if (allLines.Count > 0)
            {
                col.Item().Text("Vágólista").FontSize(11).Bold();
                col.Item().Element(ComposeCuttingTable(allLines));
            }

            // CNC operations table
            var allCnc = snapshots.SelectMany(s => s.CncInstructions).ToList();
            if (allCnc.Count > 0)
            {
                col.Item().Text("CNC műveletek").FontSize(11).Bold();
                col.Item().Element(ComposeCncTable(allCnc));
            }

            // Process steps table
            var allSteps = snapshots.SelectMany(s => s.ProcessSteps).ToList();
            if (allSteps.Count > 0)
            {
                col.Item().Text("Gyártási folyamat").FontSize(11).Bold();
                col.Item().Element(ComposeProcessTable(allSteps));
            }
        });
    };

    private static Action<IContainer> ComposeCuttingTable(List<CuttingListLine> lines) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3); // Component
                cols.RelativeColumn(2); // Anyag
                cols.RelativeColumn(1); // Sz.
                cols.RelativeColumn(1); // M.
                cols.RelativeColumn(1); // Vágás Sz.
                cols.RelativeColumn(1); // Vágás M.
                cols.RelativeColumn(1); // Vastagság
                cols.RelativeColumn(1); // Db
            });

            // Header
            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Component").Bold();
                header.Cell().Element(HeaderCell).Text("Anyag").Bold();
                header.Cell().Element(HeaderCell).Text("Sz.").Bold();
                header.Cell().Element(HeaderCell).Text("M.").Bold();
                header.Cell().Element(HeaderCell).Text("Vágás Sz.").Bold();
                header.Cell().Element(HeaderCell).Text("Vágás M.").Bold();
                header.Cell().Element(HeaderCell).Text("Vastagság").Bold();
                header.Cell().Element(HeaderCell).Text("Db").Bold();
            });

            static IContainer DataCell(IContainer c) => c.Padding(3);

            foreach (var line in lines)
            {
                var name = Truncate(line.ComponentName, MaxComponentNameLength);
                table.Cell().Element(DataCell).Text(name);
                table.Cell().Element(DataCell).Text(line.Material);
                table.Cell().Element(DataCell).Text(line.Width.ToString("F1"));
                table.Cell().Element(DataCell).Text(line.Height.ToString("F1"));
                table.Cell().Element(DataCell).Text(line.CuttingWidth.ToString("F1"));
                table.Cell().Element(DataCell).Text(line.CuttingHeight.ToString("F1"));
                table.Cell().Element(DataCell).Text(line.Thickness.ToString("F1"));
                table.Cell().Element(DataCell).Text(line.Quantity.ToString());
            }
        });
    };

    private static Action<IContainer> ComposeCncTable(List<CncInstruction> cnc) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3); // Component
                cols.RelativeColumn(2); // Művelet
                cols.RelativeColumn(2); // Pozíció
                cols.RelativeColumn(1); // Átmérő
                cols.RelativeColumn(1); // Mélység
                cols.RelativeColumn(1); // Szög
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Component").Bold();
                header.Cell().Element(HeaderCell).Text("Művelet").Bold();
                header.Cell().Element(HeaderCell).Text("Pozíció").Bold();
                header.Cell().Element(HeaderCell).Text("Átmérő").Bold();
                header.Cell().Element(HeaderCell).Text("Mélység").Bold();
                header.Cell().Element(HeaderCell).Text("Szög").Bold();
            });

            static IContainer DataCell(IContainer c) => c.Padding(3);

            foreach (var instr in cnc)
            {
                var name = Truncate(instr.ComponentName, MaxComponentNameLength);
                table.Cell().Element(DataCell).Text(name);
                table.Cell().Element(DataCell).Text(instr.Operation);
                table.Cell().Element(DataCell).Text(instr.Position ?? "—");
                table.Cell().Element(DataCell).Text(instr.Diameter?.ToString("F2") ?? "—");
                table.Cell().Element(DataCell).Text(instr.Depth?.ToString("F2") ?? "—");
                table.Cell().Element(DataCell).Text(instr.Angle?.ToString("F1") ?? "—");
            }
        });
    };

    private static Action<IContainer> ComposeProcessTable(List<ProcessStep> steps) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2); // Fázis
                cols.RelativeColumn(1); // Lépés
                cols.RelativeColumn(4); // Leírás
                cols.RelativeColumn(2); // Becsült idő
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Fázis").Bold();
                header.Cell().Element(HeaderCell).Text("Lépés").Bold();
                header.Cell().Element(HeaderCell).Text("Leírás").Bold();
                header.Cell().Element(HeaderCell).Text("Becsült idő").Bold();
            });

            static IContainer DataCell(IContainer c) => c.Padding(3);

            foreach (var step in steps.OrderBy(s => s.StepOrder))
            {
                table.Cell().Element(DataCell).Text(step.Phase);
                table.Cell().Element(DataCell).Text(step.StepOrder.ToString());
                table.Cell().Element(DataCell).Text(step.Description ?? "—");
                table.Cell().Element(DataCell).Text(FormatSeconds(step.EstimatedSeconds));
            }
        });
    };

    /// <inheritdoc/>
    public Stream GenerateManufacturingSheet(DoorOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeManufacturingHeader(order));
                page.Content().Element(ComposeManufacturingBody(order));
                page.Footer().AlignCenter().Text(
                    $"Doorstar Kft. — Gyártásilap — {DateTime.UtcNow:yyyy-MM-dd}");
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    private static Action<IContainer> ComposeManufacturingHeader(DoorOrder order) => container =>
    {
        var shortId = order.Id.ToString("N")[..8].ToUpperInvariant();
        var clientName = order.ProjectInfo?.ClientName ?? "—";
        var clientAddress = order.ProjectInfo?.ClientAddress ?? "—";
        var deliveryDate = order.ProjectInfo?.DeliveryDate?.ToString("yyyy-MM-dd") ?? "—";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        container.Column(col =>
        {
            col.Item().Text("Doorstar Kft. — Gyártásilap")
                .FontSize(16).Bold().AlignCenter();

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Rendelés: {shortId}");
                row.RelativeItem().AlignCenter().Text($"Kelt: {date}");
                row.RelativeItem().AlignRight().Text($"Szállítás: {deliveryDate}");
            });

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text($"Vevő: {clientName}");
                row.RelativeItem(2).AlignRight().Text($"Cím: {clientAddress}");
            });

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    };

    private static Action<IContainer> ComposeManufacturingBody(DoorOrder order) => container =>
    {
        var items = order.Items;
        var totalQty = items.Sum(i => i.Quantity);

        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Text("Tételek").FontSize(11).Bold();
            col.Item().Element(ComposeItemsTable(items));

            col.Item().PaddingTop(4).Text(
                $"Összesen: {items.Count} tétel, {totalQty} darab")
                .Bold();
        });
    };

    private static Action<IContainer> ComposeItemsTable(
        IReadOnlyList<Domain.Entities.DoorItem> items) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1); // Ssz.
                cols.RelativeColumn(2); // Ajtótípus
                cols.RelativeColumn(2); // Sz × M (mm)
                cols.RelativeColumn(1); // Db
                cols.RelativeColumn(2); // Anyag
                cols.RelativeColumn(2); // Felület
                cols.RelativeColumn(3); // Név/Megjegyzés
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Ssz.").Bold();
                header.Cell().Element(HeaderCell).Text("Ajtótípus").Bold();
                header.Cell().Element(HeaderCell).Text("Sz × M (mm)").Bold();
                header.Cell().Element(HeaderCell).Text("Db").Bold();
                header.Cell().Element(HeaderCell).Text("Anyag").Bold();
                header.Cell().Element(HeaderCell).Text("Felület").Bold();
                header.Cell().Element(HeaderCell).Text("Megjegyzés").Bold();
            });

            static IContainer DataCell(IContainer c) => c.Padding(3);

            foreach (var item in items)
            {
                var dims = $"{item.Dimensions.DoorWidth:F0} × {item.Dimensions.DoorHeight:F0}";
                var material = item.Materials?.FrameMaterial ?? "—";
                var surface = item.FixSide?.SurfaceType.ToString() ?? "—";
                var comment = Truncate(item.Name ?? "—", MaxComponentNameLength);

                table.Cell().Element(DataCell).Text(item.Sorszam);
                table.Cell().Element(DataCell).Text(item.DoorType.ToString());
                table.Cell().Element(DataCell).Text(dims);
                table.Cell().Element(DataCell).Text(item.Quantity.ToString());
                table.Cell().Element(DataCell).Text(material);
                table.Cell().Element(DataCell).Text(surface);
                table.Cell().Element(DataCell).Text(comment);
            }
        });
    };

    // ── Hardware List PDF ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Stream GenerateHardwareListPdf(DoorOrder order, IReadOnlyList<HardwareListItem> items)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(items);

        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeSimpleHeader(order, "Hardverlista"));
                page.Content().Element(ComposeHardwareBody(items));
                page.Footer().AlignCenter().Text(
                    $"SpaceOS — Hardverlista — {DateTime.UtcNow:yyyy-MM-dd}");
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    private static Action<IContainer> ComposeHardwareBody(IReadOnlyList<HardwareListItem> items) => container =>
    {
        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Hardverek").FontSize(11).Bold();
            col.Item().Element(ComposeHardwareTable(items));
            col.Item().PaddingTop(4).Text($"Összesen: {items.Count} tétel").Bold();
        });
    };

    private static Action<IContainer> ComposeHardwareTable(IReadOnlyList<HardwareListItem> items) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1); // Ssz.
                cols.RelativeColumn(3); // Megnevezés
                cols.RelativeColumn(2); // Típus
                cols.RelativeColumn(1); // Db
                cols.RelativeColumn(2); // Szín
                cols.RelativeColumn(3); // Megjegyzés
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);
            static IContainer DataCell(IContainer c) => c.Padding(3);

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("Ssz.").Bold();
                h.Cell().Element(HeaderCell).Text("Megnevezés").Bold();
                h.Cell().Element(HeaderCell).Text("Típus").Bold();
                h.Cell().Element(HeaderCell).Text("Db").Bold();
                h.Cell().Element(HeaderCell).Text("Szín").Bold();
                h.Cell().Element(HeaderCell).Text("Megjegyzés").Bold();
            });

            foreach (var item in items)
            {
                table.Cell().Element(DataCell).Text(item.ItemSorszam);
                table.Cell().Element(DataCell).Text(Truncate(item.Name, MaxComponentNameLength));
                table.Cell().Element(DataCell).Text(item.ComponentType);
                table.Cell().Element(DataCell).Text(item.Quantity.ToString());
                table.Cell().Element(DataCell).Text(item.Color);
                table.Cell().Element(DataCell).Text(item.Note ?? "—");
            }
        });
    };

    // ── Material Requirements PDF ─────────────────────────────────────────────

    /// <inheritdoc/>
    public Stream GenerateMaterialReqPdf(DoorOrder order, IReadOnlyList<MaterialRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(requirements);

        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeSimpleHeader(order, "Anyagnorma"));
                page.Content().Element(ComposeMaterialBody(requirements));
                page.Footer().AlignCenter().Text(
                    $"SpaceOS — Anyagnorma — {DateTime.UtcNow:yyyy-MM-dd}");
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    private static Action<IContainer> ComposeMaterialBody(IReadOnlyList<MaterialRequirement> requirements) => container =>
    {
        var totalM2 = requirements.Sum(r => r.TotalM2);
        var totalLinear = requirements.Sum(r => r.TotalLinearMeter);

        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Anyagszükséglet").FontSize(11).Bold();
            col.Item().Element(ComposeMaterialTable(requirements));
            col.Item().PaddingTop(4).Column(summary =>
            {
                summary.Item().Text($"Összesen: {requirements.Count} anyag").Bold();
                if (totalM2 > 0)
                    summary.Item().Text($"Lapanyag összesen: {totalM2:F3} m²");
                if (totalLinear > 0)
                    summary.Item().Text($"Élzáró összesen: {totalLinear:F3} fm");
            });
        });
    };

    private static Action<IContainer> ComposeMaterialTable(IReadOnlyList<MaterialRequirement> requirements) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3); // Anyag
                cols.RelativeColumn(1); // Vastagság
                cols.RelativeColumn(2); // Terület (m²)
                cols.RelativeColumn(2); // Él (fm)
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);
            static IContainer DataCell(IContainer c) => c.Padding(3);

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("Anyag").Bold();
                h.Cell().Element(HeaderCell).Text("Vastagság").Bold();
                h.Cell().Element(HeaderCell).Text("Terület (m²)").Bold();
                h.Cell().Element(HeaderCell).Text("Él (fm)").Bold();
            });

            foreach (var req in requirements)
            {
                table.Cell().Element(DataCell).Text(req.Material);
                table.Cell().Element(DataCell).Text(req.Thickness > 0 ? $"{req.Thickness:F1} mm" : "—");
                table.Cell().Element(DataCell).Text(req.TotalM2 > 0 ? $"{req.TotalM2:F3}" : "—");
                table.Cell().Element(DataCell).Text(req.TotalLinearMeter > 0 ? $"{req.TotalLinearMeter:F3}" : "—");
            }
        });
    };

    // ── Shared header helper ──────────────────────────────────────────────────

    private static Action<IContainer> ComposeSimpleHeader(DoorOrder order, string title) => container =>
    {
        var shortId = order.Id.ToString("N")[..8].ToUpperInvariant();
        var clientName = order.ProjectInfo?.ClientName ?? "—";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        container.Column(col =>
        {
            col.Item().Text($"SpaceOS — {title}")
                .FontSize(14).Bold().AlignCenter();

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Rendelés: {shortId}");
                row.RelativeItem().AlignCenter().Text($"Vevő: {clientName}");
                row.RelativeItem().AlignRight().Text($"Kelt: {date}");
            });

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    };

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    private static string FormatSeconds(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalMinutes >= 1
            ? $"{(int)ts.TotalMinutes} perc"
            : $"{seconds} mp";
    }
}
