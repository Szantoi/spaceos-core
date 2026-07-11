using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

/// <summary>
/// Edge-case validation tests for <see cref="DoorDimensions.Create"/> —
/// covering max press size limits and boundary values.
/// These extend the basic tests in <see cref="DoorDimensionsTests"/>.
/// </summary>
public class DoorDimensionValidationTests
{
    // ── Invalid width scenarios ───────────────────────────────────────────────

    [Theory]
    [InlineData(0, "zero width")]
    [InlineData(-100, "negative width")]
    [InlineData(99999, "exceeds max press size")]
    public void Create_WithInvalidWidth_ReturnsInvalid(int width, string scenario)
    {
        // Arrange — wall opening must be >= door width to pass that check first,
        // except for 0/negative which fail the positive-check first
        var wallWidth = width > 0 ? (decimal)width + 1 : 900m;

        // Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: wallWidth,
            doorWidth: (decimal)width,
            wallOpeningHeight: 3100m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeFalse(because: scenario);
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorWidth",
            because: $"DoorWidth validation should fire for scenario: {scenario}");
    }

    [Theory]
    [InlineData(0, "zero height")]
    [InlineData(-1, "negative height")]
    [InlineData(99999, "exceeds max press height")]
    public void Create_WithInvalidHeight_ReturnsInvalid(int height, string scenario)
    {
        // Arrange — wall opening must be >= door height to pass that check first,
        // except for 0/negative which fail the positive-check first
        var wallHeight = height > 0 ? (decimal)height + 1 : 2100m;

        // Act
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 850m,
            wallOpeningHeight: wallHeight,
            doorHeight: (decimal)height,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        // Assert
        result.IsSuccess.Should().BeFalse(because: scenario);
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorHeight",
            because: $"DoorHeight validation should fire for scenario: {scenario}");
    }

    // ── Boundary: exactly at max ───────────────────────────────────────────────

    [Fact]
    public void Create_WithWidthAtMaxPressLimit_Succeeds()
    {
        // doorWidth = 2600 (exactly at limit) must be accepted
        var result = DoorDimensions.Create(
            wallOpeningWidth: 2600m,
            doorWidth: 2600m,
            wallOpeningHeight: 3100m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        result.IsSuccess.Should().BeTrue(because: "2600 mm is exactly the maximum press width and must be valid");
        result.Value.DoorWidth.Should().Be(2600m);
    }

    [Fact]
    public void Create_WithHeightAtMaxPressLimit_Succeeds()
    {
        // doorHeight = 3000 (exactly at limit) must be accepted
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 850m,
            wallOpeningHeight: 3000m,
            doorHeight: 3000m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        result.IsSuccess.Should().BeTrue(because: "3000 mm is exactly the maximum press height and must be valid");
        result.Value.DoorHeight.Should().Be(3000m);
    }

    // ── Boundary: one unit over max ───────────────────────────────────────────

    [Fact]
    public void Create_WithWidthJustOverMaxPressLimit_ReturnsInvalid()
    {
        // doorWidth = 2601 — one mm over the industry press limit
        var result = DoorDimensions.Create(
            wallOpeningWidth: 2700m,
            doorWidth: 2601m,
            wallOpeningHeight: 3100m,
            doorHeight: 2050m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        result.IsSuccess.Should().BeFalse(because: "2601 mm exceeds the maximum press width of 2600 mm");
        result.ValidationErrors.Should().Contain(e =>
            e.Identifier == "DoorWidth" && e.ErrorMessage.Contains("2600"));
    }

    [Fact]
    public void Create_WithHeightJustOverMaxPressLimit_ReturnsInvalid()
    {
        // doorHeight = 3001 — one mm over the industry press limit
        var result = DoorDimensions.Create(
            wallOpeningWidth: 900m,
            doorWidth: 850m,
            wallOpeningHeight: 3100m,
            doorHeight: 3001m,
            wallOpeningThickness: 200m,
            doorThickness: 180m);

        result.IsSuccess.Should().BeFalse(because: "3001 mm exceeds the maximum press height of 3000 mm");
        result.ValidationErrors.Should().Contain(e =>
            e.Identifier == "DoorHeight" && e.ErrorMessage.Contains("3000"));
    }
}
