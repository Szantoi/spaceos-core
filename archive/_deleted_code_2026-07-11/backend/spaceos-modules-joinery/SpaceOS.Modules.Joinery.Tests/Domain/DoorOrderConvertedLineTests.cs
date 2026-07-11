using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

public class DoorOrderConvertedLineTests
{
    // DoorOrderConvertedLine.Create is internal — accessible via InternalsVisibleTo
    // declared in SpaceOS.Modules.Joinery.Domain.csproj.

    [Fact]
    public void Create_ValidArgs_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = DoorOrderConvertedLine.Create(
            id, null, "Standard white door",
            quantity: 2m, unitPriceNet: 150m, vatRate: 0.27m,
            discountPercent: null, sortOrder: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.Description.Should().Be("Standard white door");
        result.Value.Quantity.Should().Be(2m);
        result.Value.UnitPriceNet.Should().Be(150m);
        result.Value.VatRate.Should().Be(0.27m);
        result.Value.DiscountPercent.Should().BeNull();
        result.Value.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_QuantityZero_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorOrderConvertedLine.Create(
            Guid.NewGuid(), null, "Door",
            quantity: 0m, unitPriceNet: 100m, vatRate: 0.27m,
            discountPercent: null, sortOrder: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Quantity");
    }

    [Fact]
    public void Create_VatRateAboveOne_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorOrderConvertedLine.Create(
            Guid.NewGuid(), null, "Door",
            quantity: 1m, unitPriceNet: 100m, vatRate: 1.1m,
            discountPercent: null, sortOrder: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "VatRate");
    }

    [Fact]
    public void Create_DiscountPercentAbove100_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorOrderConvertedLine.Create(
            Guid.NewGuid(), null, "Door",
            quantity: 1m, unitPriceNet: 100m, vatRate: 0.27m,
            discountPercent: 101m, sortOrder: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DiscountPercent");
    }

    [Fact]
    public void Create_DescriptionTooLong_ReturnsInvalid()
    {
        // Arrange
        var tooLong = new string('a', 501);

        // Act
        var result = DoorOrderConvertedLine.Create(
            Guid.NewGuid(), null, tooLong,
            quantity: 1m, unitPriceNet: 100m, vatRate: 0.27m,
            discountPercent: null, sortOrder: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Description");
    }

    [Fact]
    public void Create_NegativeUnitPrice_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorOrderConvertedLine.Create(
            Guid.NewGuid(), null, "Door",
            quantity: 1m, unitPriceNet: -0.01m, vatRate: 0.27m,
            discountPercent: null, sortOrder: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "UnitPriceNet");
    }
}
