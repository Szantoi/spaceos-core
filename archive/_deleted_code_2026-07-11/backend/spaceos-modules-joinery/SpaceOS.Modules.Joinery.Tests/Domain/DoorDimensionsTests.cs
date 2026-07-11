using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

public class DoorDimensionsTests
{
    [Fact]
    public void Create_WithValidDimensions_Succeeds()
    {
        // Arrange & Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 850m,
            wallOpeningHeight: 2100m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DoorWidth.Should().Be(850m);
        result.Value.DoorHeight.Should().Be(2050m);
    }

    [Fact]
    public void Create_WithZeroWidth_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 0m,
            wallOpeningHeight: 2100m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorWidth");
    }

    [Fact]
    public void Create_WithDoorWiderThanWallOpening_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: 800m,
            doorWidth: 850m,
            wallOpeningHeight: 2100m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorWidth");
    }

    [Fact]
    public void Create_WithDoorTallerThanWallOpening_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 850m,
            wallOpeningHeight: 2000m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorHeight");
    }

    [Fact]
    public void Create_WithNegativeThickness_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 850m,
            wallOpeningHeight: 2100m,
            doorHeight: -1m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorHeight");
    }
}
