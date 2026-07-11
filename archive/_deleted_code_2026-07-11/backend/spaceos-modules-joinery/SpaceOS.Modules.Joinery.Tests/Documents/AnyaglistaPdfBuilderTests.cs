using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Infrastructure.Documents;

namespace SpaceOS.Modules.Joinery.Tests.Documents;

/// <summary>
/// Tests for AnyaglistaPdfBuilder — verifies PDF generation produces valid output.
/// Uses the real QuestPDF builder (community license, no external dependencies).
/// </summary>
public class AnyaglistaPdfBuilderTests
{
    private static readonly AnyaglistaPdfBuilder Builder = new();

    private static AnyaglistaData CreateData(int rowCount = 3) =>
        new(
            OrderId: Guid.NewGuid(),
            CustomerName: "Doorstar Kft.",
            GeneratedAt: new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero),
            Rows: Enumerable.Range(1, rowCount)
                .Select(i => new AnyaglistaRow(
                    MaterialType: $"18mm MDF {i}",
                    SupplierCode: $"MDF-18-{i:D4}",
                    Quantity: i * 2.5m,
                    Unit: "m2",
                    Notes: i % 2 == 0 ? $"Note {i}" : null))
                .ToList());

    [Fact]
    public void GeneratePdf_WithValidData_ReturnsNonEmptyBytes()
    {
        var data = CreateData();

        var pdfBytes = Builder.GeneratePdf(data);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_MultipleRows_ReturnsValidPdf()
    {
        var data = CreateData(rowCount: 10);

        var pdfBytes = Builder.GeneratePdf(data);

        // Valid PDF starts with %PDF header (0x25 0x50 0x44 0x46)
        pdfBytes.Should().HaveCountGreaterThan(4);
        pdfBytes[0].Should().Be(0x25); // %
        pdfBytes[1].Should().Be(0x50); // P
        pdfBytes[2].Should().Be(0x44); // D
        pdfBytes[3].Should().Be(0x46); // F
    }

    [Fact]
    public void GeneratePdf_EmptyRows_ReturnsValidPdf()
    {
        var data = CreateData(rowCount: 0);

        var pdfBytes = Builder.GeneratePdf(data);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_SingleRow_ReturnsValidPdf()
    {
        var data = CreateData(rowCount: 1);

        var pdfBytes = Builder.GeneratePdf(data);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_WithNullData_ThrowsArgumentNullException()
    {
        var act = () => Builder.GeneratePdf(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
