using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Pdf;
using UglyToad.PdfPig;

namespace SpaceOS.Modules.Joinery.Tests.Pdf;

public class HardwareListPdfGeneratorTests
{
    private readonly ProductionSheetGenerator _sut = new();

    private static DoorOrder BuildOrder()
    {
        var order = DoorOrder.Create(Guid.NewGuid(), "PRJ-HW", "Hardver Teszt", Guid.NewGuid()).Value;
        var info = ProjectInfo.Create("Hardver Kft.", "Budapest, Hw u. 5.", null, null).Value;
        order.SetProjectInfo(info);
        return order;
    }

    private static IReadOnlyList<HardwareListItem> BuildItems() =>
        new List<HardwareListItem>
        {
            new("A001", "Zsanér", "Ajtózsanér", 3, "Ezüst", null),
            new("A001", "Zár", "Hengerzár", 1, "Arany", "Jobbos"),
        };

    private static string ExtractText(Stream stream)
    {
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        using var pdf = PdfDocument.Open(bytes);
        return string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));
    }

    [Fact]
    public void GenerateHardwareListPdf_ReturnsNonEmptyStream()
    {
        var stream = _sut.GenerateHardwareListPdf(BuildOrder(), BuildItems());

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateHardwareListPdf_ContainsOrderShortId()
    {
        var order = BuildOrder();
        var shortId = order.Id.ToString("N")[..8].ToUpperInvariant();

        var stream = _sut.GenerateHardwareListPdf(order, BuildItems());
        var text = ExtractText(stream);

        text.Should().Contain(shortId);
    }

    [Fact]
    public void GenerateHardwareListPdf_ContainsHardwaritelLabel()
    {
        var stream = _sut.GenerateHardwareListPdf(BuildOrder(), BuildItems());
        var text = ExtractText(stream);

        text.Should().Contain("Hardver",
            because: "the PDF title must contain 'Hardverlista'");
    }

    [Fact]
    public void GenerateHardwareListPdf_EmptyItems_ReturnsValidPdf()
    {
        var stream = _sut.GenerateHardwareListPdf(BuildOrder(), new List<HardwareListItem>());

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0, because: "even an empty hardware list must produce a valid PDF");
    }
}
