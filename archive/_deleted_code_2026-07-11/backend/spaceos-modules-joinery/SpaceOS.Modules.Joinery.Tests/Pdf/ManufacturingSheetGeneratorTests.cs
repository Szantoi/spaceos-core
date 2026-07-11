using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Pdf;
using UglyToad.PdfPig;

namespace SpaceOS.Modules.Joinery.Tests.Pdf;

/// <summary>
/// Tests for <see cref="ProductionSheetGenerator.GenerateManufacturingSheet"/>.
/// Uses PdfPig to extract text and verify content.
/// </summary>
public class ManufacturingSheetGeneratorTests
{
    private readonly ProductionSheetGenerator _sut = new();

    private static DoorOrder BuildOrderWithItems()
    {
        var tenantId = Guid.NewGuid();
        var order = DoorOrder.Create(tenantId, "PRJ-MFG", "Gyártásilap Teszt", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 860m, 2100m, 2060m, 120m, 40m).Value;
        var item = DoorItem.Create(order.Id, "A001", 2, DoorType.FAF_T, OpeningDirection.Left, dims);
        item.SetName("Falcos beltéri ajtó");
        order.AddItem(item);
        return order;
    }

    private static string ExtractText(Stream stream)
    {
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        using var pdf = PdfDocument.Open(bytes);
        return string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));
    }

    [Fact]
    public void GenerateManufacturingSheet_ReturnsNonEmptyStream()
    {
        var order = BuildOrderWithItems();

        var stream = _sut.GenerateManufacturingSheet(order);

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateManufacturingSheet_ContainsOrderShortId()
    {
        var order = BuildOrderWithItems();
        var shortId = order.Id.ToString("N")[..8].ToUpperInvariant();

        var stream = _sut.GenerateManufacturingSheet(order);
        var text = ExtractText(stream);

        text.Should().Contain(shortId,
            because: "the manufacturing sheet header must contain the 8-char order ID");
    }

    [Fact]
    public void GenerateManufacturingSheet_ContainsDoorstarTitle()
    {
        var order = BuildOrderWithItems();

        var stream = _sut.GenerateManufacturingSheet(order);
        var text = ExtractText(stream);

        text.Should().Contain("Doorstar",
            because: "the manufacturing sheet header must show 'Doorstar Kft.'");
    }

    [Fact]
    public void GenerateManufacturingSheet_ContainsItemSorszam()
    {
        var order = BuildOrderWithItems();

        var stream = _sut.GenerateManufacturingSheet(order);
        var text = ExtractText(stream);

        text.Should().Contain("A001",
            because: "the items table must include the sorszam of each DoorItem");
    }

    [Fact]
    public void GenerateManufacturingSheet_WithProjectInfo_ContainsClientName()
    {
        var tenantId = Guid.NewGuid();
        var order = DoorOrder.Create(tenantId, "PRJ-CLI", "Ügyfél Teszt", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 860m, 2100m, 2060m, 120m, 40m).Value;
        order.AddItem(DoorItem.Create(order.Id, "B001", 1, DoorType.FAF_T, OpeningDirection.Right, dims));
        var info = ProjectInfo.Create("Teszt Béla", "Budapest, Fő u. 1.", null,
            new DateOnly(2026, 6, 30)).Value;
        order.SetProjectInfo(info);

        var stream = _sut.GenerateManufacturingSheet(order);
        var text = ExtractText(stream);

        text.Should().Contain("Teszt",
            because: "the header must include the client name from ProjectInfo");
    }
}
