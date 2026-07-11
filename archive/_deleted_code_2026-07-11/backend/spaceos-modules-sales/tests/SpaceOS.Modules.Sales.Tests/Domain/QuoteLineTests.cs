using FluentAssertions;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Domain;

public class QuoteLineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArgs_ComputesTotals()
    {
        var result = QuoteLine.Create(
            TenantId, QuoteLineType.Product, null,
            "Teszt termék", quantity: 2m,
            unitPrice: new Money(1000m, "HUF"),
            vatRate: 0.27m, discountPercent: null, sortOrder: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.LineNet.Amount.Should().Be(2000m);
        result.Value.LineVat.Amount.Should().Be(540m);
        result.Value.LineGross.Amount.Should().Be(2540m);
    }

    [Fact]
    public void Create_WithDiscount_ReducesNet()
    {
        var result = QuoteLine.Create(
            TenantId, QuoteLineType.Product, null,
            "Kedvezményes", quantity: 1m,
            unitPrice: new Money(1000m, "HUF"),
            vatRate: 0m, discountPercent: 0.10m, sortOrder: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.LineNet.Amount.Should().Be(900m);
    }

    [Fact]
    public void Create_ZeroQuantity_ReturnsInvalid()
    {
        var result = QuoteLine.Create(
            TenantId, QuoteLineType.Product, null,
            "Teszt", quantity: 0m,
            unitPrice: new Money(100m, "HUF"),
            vatRate: 0.27m, discountPercent: null, sortOrder: 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_NegativeUnitPrice_ReturnsInvalid()
    {
        var result = QuoteLine.Create(
            TenantId, QuoteLineType.Product, null,
            "Negatív", quantity: 1m,
            unitPrice: new Money(-1m, "HUF"),
            vatRate: 0.27m, discountPercent: null, sortOrder: 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_VatRateOutOfRange_ReturnsInvalid()
    {
        var result = QuoteLine.Create(
            TenantId, QuoteLineType.Product, null,
            "Helytelen ÁFA", quantity: 1m,
            unitPrice: new Money(1000m, "HUF"),
            vatRate: 1.5m, discountPercent: null, sortOrder: 1);

        result.IsSuccess.Should().BeFalse();
    }
}
