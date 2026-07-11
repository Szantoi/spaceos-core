using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Application.Products.Services;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

/// <summary>
/// QuestPDF-based work order PDF generator.
/// </summary>
public sealed class WorkOrderPdfService : IWorkOrderPdfService
{
    private readonly IWorkOrderRepository _workOrderRepository;

    public WorkOrderPdfService(IWorkOrderRepository workOrderRepository)
    {
        _workOrderRepository = workOrderRepository;
    }

    public Task<string> GenerateWorkOrderPdfAsync(WorkOrder workOrder, ProductConfiguration config, CancellationToken ct)
    {
        // In Phase 1, we return a URL pattern. The actual PDF is generated on-demand.
        var url = $"/api/work-orders/{workOrder.Id}/sheet.pdf";
        return Task.FromResult(url);
    }

    public async Task<Stream?> GetWorkOrderPdfStreamAsync(Guid workOrderId, Guid tenantId, CancellationToken ct)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, tenantId, ct).ConfigureAwait(false);
        if (workOrder is null)
            return null;

        return GeneratePdf(workOrder);
    }

    private static Stream GeneratePdf(WorkOrder workOrder)
    {
        var bomItems = ParseBomItems(workOrder.BomItems);
        var configParams = ParseConfigParams(workOrder);

        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader(workOrder));
                page.Content().Element(ComposeBody(workOrder, bomItems, configParams));
                page.Footer().AlignCenter().Text(
                    $"SpaceOS — Gyártási lap — {DateTime.UtcNow:yyyy-MM-dd}");
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    private static Action<IContainer> ComposeHeader(WorkOrder workOrder) => container =>
    {
        var shortId = workOrder.Id.ToString("N")[..8].ToUpperInvariant();
        var customerRef = workOrder.CustomerRef ?? "—";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        container.Column(col =>
        {
            col.Item().Text("SpaceOS — Gyártási munkalap")
                .FontSize(16).Bold().AlignCenter();

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Work Order: WO-{shortId}");
                row.RelativeItem().AlignCenter().Text($"Vevő ref.: {customerRef}");
                row.RelativeItem().AlignRight().Text($"Kelt: {date}");
            });

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text($"Szállítás: {workOrder.DeliveryDate:yyyy-MM-dd}");
                row.RelativeItem().AlignCenter().Text($"Darabszám: {workOrder.Quantity}");
                row.RelativeItem().AlignRight().Text($"Kezdés: {workOrder.ScheduledStart:yyyy-MM-dd}");
            });

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    };

    private static Action<IContainer> ComposeBody(
        WorkOrder workOrder,
        IReadOnlyList<WorkOrderBomItem> bomItems,
        ConfigParams? configParams) => container =>
    {
        container.Column(col =>
        {
            col.Spacing(12);

            // Configuration details
            if (configParams != null)
            {
                col.Item().Text("Konfiguráció").FontSize(11).Bold();
                col.Item().Element(ComposeConfigTable(configParams));
            }

            // BOM table
            col.Item().Text("Anyagjegyzék (BOM)").FontSize(11).Bold();
            col.Item().Element(ComposeBomTable(bomItems));

            // Production steps
            col.Item().Text("Gyártási lépések").FontSize(11).Bold();
            col.Item().Element(ComposeProductionSteps());

            // QC checkpoints
            col.Item().Text("Minőségellenőrzési pontok").FontSize(11).Bold();
            col.Item().Element(ComposeQcCheckpoints());

            // Notes
            if (!string.IsNullOrEmpty(workOrder.Notes))
            {
                col.Item().Text("Megjegyzések").FontSize(11).Bold();
                col.Item().Padding(4).Background(Colors.Grey.Lighten4).Text(workOrder.Notes);
            }

            // Summary
            col.Item().PaddingTop(8).Column(summary =>
            {
                summary.Item().Text($"Anyagköltség: {workOrder.TotalMaterialCost:N0} Ft").Bold();
                summary.Item().Text($"Munkadíj: {workOrder.EstimatedLabor:N0} Ft");
                summary.Item().Text($"Összesen: {workOrder.TotalCost:N0} Ft").FontSize(11).Bold();
            });
        });
    };

    private static Action<IContainer> ComposeConfigTable(ConfigParams config) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
            });

            static IContainer Cell(IContainer c) => c.Padding(3);

            table.Cell().Element(Cell).Text("Szélesség:").Bold();
            table.Cell().Element(Cell).Text($"{config.Width:F0} mm");
            table.Cell().Element(Cell).Text("Magasság:").Bold();
            table.Cell().Element(Cell).Text($"{config.Height:F0} mm");

            table.Cell().Element(Cell).Text("Vastagság:").Bold();
            table.Cell().Element(Cell).Text($"{config.Thickness:F0} mm");
            table.Cell().Element(Cell).Text("Mag anyag:").Bold();
            table.Cell().Element(Cell).Text(config.Core ?? "—");

            table.Cell().Element(Cell).Text("Furnér:").Bold();
            table.Cell().Element(Cell).Text(config.Veneer ?? "—");
            table.Cell().Element(Cell).Text("Élzáró:").Bold();
            table.Cell().Element(Cell).Text(config.Edge ?? "—");

            table.Cell().Element(Cell).Text("Zsanér:").Bold();
            table.Cell().Element(Cell).Text(config.Hinge ?? "—");
            table.Cell().Element(Cell).Text("Kilincs:").Bold();
            table.Cell().Element(Cell).Text(config.Handle ?? "—");
        });
    };

    private static Action<IContainer> ComposeBomTable(IReadOnlyList<WorkOrderBomItem> items) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3); // Name
                cols.RelativeColumn(1); // Quantity
                cols.RelativeColumn(1); // Unit
                cols.RelativeColumn(2); // Total Price
                cols.RelativeColumn(2); // Supplier
                cols.RelativeColumn(1); // In Stock
                cols.RelativeColumn(1); // To Order
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Megnevezés").Bold();
                header.Cell().Element(HeaderCell).Text("Menny.").Bold();
                header.Cell().Element(HeaderCell).Text("Egys.").Bold();
                header.Cell().Element(HeaderCell).Text("Ár (Ft)").Bold();
                header.Cell().Element(HeaderCell).Text("Beszállító").Bold();
                header.Cell().Element(HeaderCell).Text("Készl.").Bold();
                header.Cell().Element(HeaderCell).Text("Rend.").Bold();
            });

            static IContainer DataCell(IContainer c) => c.Padding(3);

            foreach (var item in items)
            {
                var name = item.Name.Length > 40 ? item.Name[..40] + "..." : item.Name;
                table.Cell().Element(DataCell).Text(name);
                table.Cell().Element(DataCell).Text(item.Quantity.ToString("F1"));
                table.Cell().Element(DataCell).Text(item.Unit);
                table.Cell().Element(DataCell).Text(item.TotalPrice.ToString("N0"));
                table.Cell().Element(DataCell).Text(item.Supplier ?? "—");
                table.Cell().Element(DataCell).Text(item.InStock.ToString());
                table.Cell().Element(DataCell).Text(item.ToOrder > 0 ? item.ToOrder.ToString() : "—");
            }
        });
    };

    private static Action<IContainer> ComposeProductionSteps() => container =>
    {
        var steps = new[]
        {
            ("1", "Anyag előkészítés", "Alapanyagok komissiózása és ellenőrzése"),
            ("2", "Szabászat", "Lapok és élzárók vágása méretre"),
            ("3", "Élzárás", "Él ragasztása és finomcsiszolása"),
            ("4", "Fúrások", "Zsanér és kilincs helyének előfúrása"),
            ("5", "Összeszerelés", "Vasalatok felszerelése"),
            ("6", "Végső QC", "Minőségellenőrzés és csomagolás")
        };

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1);
                cols.RelativeColumn(2);
                cols.RelativeColumn(5);
                cols.RelativeColumn(1);
            });

            static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3).Padding(3);

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("#").Bold();
                header.Cell().Element(HeaderCell).Text("Művelet").Bold();
                header.Cell().Element(HeaderCell).Text("Leírás").Bold();
                header.Cell().Element(HeaderCell).Text("✓").Bold();
            });

            static IContainer DataCell(IContainer c) => c.Padding(3);

            foreach (var (num, name, desc) in steps)
            {
                table.Cell().Element(DataCell).Text(num);
                table.Cell().Element(DataCell).Text(name);
                table.Cell().Element(DataCell).Text(desc);
                table.Cell().Element(DataCell).Text("☐");
            }
        });
    };

    private static Action<IContainer> ComposeQcCheckpoints() => container =>
    {
        var checkpoints = new[]
        {
            "Méretek megfelelnek a specifikációnak (±1mm)",
            "Élzáró tapadása megfelelő",
            "Felület sérülésmentes",
            "Vasalatok működőképesek",
            "Csomagolás épségvédelme biztosított"
        };

        container.Column(col =>
        {
            foreach (var cp in checkpoints)
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(20).Text("☐");
                    row.RelativeItem().Text(cp);
                });
            }
        });
    };

    private static IReadOnlyList<WorkOrderBomItem> ParseBomItems(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<WorkOrderBomItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<WorkOrderBomItem>();
        }
        catch
        {
            return new List<WorkOrderBomItem>();
        }
    }

    private static ConfigParams? ParseConfigParams(WorkOrder workOrder)
    {
        // We don't have direct access to config params in WorkOrder
        // This would require joining with ProductConfiguration
        // For now, return null - can be enhanced in Phase 2
        return null;
    }

    private sealed record ConfigParams(
        decimal Width,
        decimal Height,
        decimal Thickness,
        string? Core,
        string? Veneer,
        string? Edge,
        string? Hinge,
        string? Handle
    );
}
