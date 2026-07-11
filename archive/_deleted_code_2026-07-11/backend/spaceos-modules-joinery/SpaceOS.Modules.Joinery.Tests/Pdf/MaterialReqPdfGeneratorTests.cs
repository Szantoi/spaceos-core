using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Infrastructure.Pdf;
using UglyToad.PdfPig;

namespace SpaceOS.Modules.Joinery.Tests.Pdf;

public class MaterialReqPdfGeneratorTests
{
    private readonly ProductionSheetGenerator _sut = new();

    private static DoorOrder BuildOrder()
        => DoorOrder.Create(Guid.NewGuid(), "PRJ-MAT", "Anyag Teszt", Guid.NewGuid()).Value;

    private static IReadOnlyList<MaterialRequirement> BuildRequirements() =>
        new List<MaterialRequirement>
        {
            new("MDF 18mm", 18m, 4.320m, 0m),
            new("HDF 8mm", 8m, 1.200m, 0m),
            new("Élzáró ABS 1mm", 0m, 0m, 12.500m),
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
    public void GenerateMaterialReqPdf_ReturnsNonEmptyStream()
    {
        var stream = _sut.GenerateMaterialReqPdf(BuildOrder(), BuildRequirements());

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateMaterialReqPdf_ContainsOrderShortId()
    {
        var order = BuildOrder();
        var shortId = order.Id.ToString("N")[..8].ToUpperInvariant();

        var stream = _sut.GenerateMaterialReqPdf(order, BuildRequirements());
        var text = ExtractText(stream);

        text.Should().Contain(shortId);
    }

    [Fact]
    public void GenerateMaterialReqPdf_ContainsMaterialLabel()
    {
        var stream = _sut.GenerateMaterialReqPdf(BuildOrder(), BuildRequirements());
        var text = ExtractText(stream);

        text.Should().Contain("Anyag",
            because: "the PDF must contain the 'Anyag' column header");
    }

    [Fact]
    public void GenerateMaterialReqPdf_EmptyRequirements_ReturnsValidPdf()
    {
        var stream = _sut.GenerateMaterialReqPdf(BuildOrder(), new List<MaterialRequirement>());

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0, because: "empty requirements must still produce a valid PDF");
    }
}
