using FluentAssertions;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

/// <summary>
/// Domain tests for Anyaglista aggregate — creation and PDF assignment.
/// </summary>
public class AnyaglistaTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly byte[] FakePdf = [0x25, 0x50, 0x44, 0x46]; // %PDF

    [Fact]
    public void Create_SetsCorrectFields()
    {
        var anyaglista = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista.Create(OrderId, TenantId);

        anyaglista.OrderId.Should().Be(OrderId);
        anyaglista.TenantId.Should().Be(TenantId);
        anyaglista.Id.Should().NotBeEmpty();
        anyaglista.PdfContent.Should().BeNull();
        anyaglista.StorageUrl.Should().BeNull();
        anyaglista.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetPdf_AssignsPdfContentAndStorageUrl()
    {
        var anyaglista = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista.Create(OrderId, TenantId);
        const string url = "anyaglista/tenant/order/anyaglista.pdf";

        anyaglista.SetPdf(FakePdf, url);

        anyaglista.PdfContent.Should().Equal(FakePdf);
        anyaglista.StorageUrl.Should().Be(url);
    }

    [Fact]
    public void SetPdf_CalledTwice_OverwritesPreviousContent()
    {
        var anyaglista = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista.Create(OrderId, TenantId);
        var firstPdf = new byte[] { 1, 2, 3 };
        var secondPdf = new byte[] { 4, 5, 6 };

        anyaglista.SetPdf(firstPdf, "url1");
        anyaglista.SetPdf(secondPdf, "url2");

        anyaglista.PdfContent.Should().Equal(secondPdf);
        anyaglista.StorageUrl.Should().Be("url2");
    }

    [Fact]
    public void SetPdf_WithNullContent_ThrowsArgumentNullException()
    {
        var anyaglista = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista.Create(OrderId, TenantId);

        var act = () => anyaglista.SetPdf(null!, "url");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetPdf_WithEmptyUrl_ThrowsArgumentException()
    {
        var anyaglista = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista.Create(OrderId, TenantId);

        var act = () => anyaglista.SetPdf(FakePdf, string.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Storage URL*");
    }
}
