using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Services;

namespace SpaceOS.Modules.Joinery.Tests.Calculation;

public class DoorCalculationServiceTests
{
    private readonly DoorCalculationService _sut = new();

    private static DoorItem CreateItem(
        DoorType doorType = DoorType.FAF_T,
        decimal wallW = 900m, decimal doorW = 850m,
        decimal wallH = 2100m, decimal doorH = 2050m,
        int quantity = 1,
        string sorszam = "A01")
    {
        var dims = DoorDimensions.Create(wallW, doorW, wallH, doorH, 200m, 180m).Value;
        return DoorItem.Create(Guid.NewGuid(), sorszam, quantity, doorType, OpeningDirection.Left, dims);
    }

    private static DoorTypeRule CreateRule(
        string doorType = "FAF_T",
        decimal bkmWidthFixed = 8m,
        decimal bkmHeightFixed = 4m)
    {
        return new DoorTypeRule
        {
            DoorType = doorType,
            BkmWidthFixed = bkmWidthFixed,
            BkmHeightFixed = bkmHeightFixed
        };
    }

    private static PartDimensionRule CreateDimRule(
        string doorType = "FAF_T",
        string componentName = "Belso keret-szeles",
        decimal widthBase = 0m,
        decimal widthMultiplier = 1m,
        decimal lengthBase = 0m,
        decimal lengthMultiplier = 1m,
        int quantity = 1,
        string componentType = "Keret")
    {
        return new PartDimensionRule
        {
            Id = Guid.NewGuid(),
            DoorType = doorType,
            ComponentName = componentName,
            ComponentType = componentType,
            Material = "MDF",
            Thickness = 18m,
            Quantity = quantity,
            WidthBase = widthBase,
            WidthMultiplierFactor = widthMultiplier,
            LengthBase = lengthBase,
            LengthMultiplierFactor = lengthMultiplier
        };
    }

    private static GlobalConstant CreateOversize(decimal value = 1m) =>
        new GlobalConstant { Key = "CuttingOversize", Value = value };

    [Fact]
    public void CalculateCuttingList_FAF_T_AppliesBkmWidthFixed()
    {
        // Arrange
        // DoorWidth=850, BkmWidthFixed=8 → bkmWidthFixed=858
        // WidthBase=0, WidthMultiplierFactor=1 → width = 0 + 1*858 + 1(oversize) = 859
        var item = CreateItem(doorW: 850m);
        var rule = CreateRule(bkmWidthFixed: 8m);
        var dimRule = CreateDimRule(widthBase: 0m, widthMultiplier: 1m);
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result.Should().HaveCount(1);
        result[0].Width.Should().Be(859m); // 850 + 8 + 1
    }

    [Fact]
    public void CalculateCuttingList_AppliesCuttingOversize()
    {
        // Arrange
        // CuttingOversize=1 is applied to both width and length
        var item = CreateItem(doorW: 800m, doorH: 2000m);
        var rule = CreateRule(bkmWidthFixed: 0m, bkmHeightFixed: 0m);
        var dimRule = CreateDimRule(widthBase: 0m, widthMultiplier: 1m, lengthBase: 0m, lengthMultiplier: 1m);
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result[0].Width.Should().Be(801m); // 800 + 0*0 + 1
        result[0].Length.Should().Be(2001m); // 2000 + 0*0 + 1
    }

    [Fact]
    public void CalculateCuttingList_MultipleItems_CorrectQuantity()
    {
        // Arrange
        // item.Quantity=3, dimRule.Quantity=2 → result quantity = 6
        var item = CreateItem(quantity: 3);
        var rule = CreateRule();
        var dimRule = CreateDimRule(quantity: 2);
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result[0].Quantity.Should().Be(6);
    }

    [Fact]
    public void CalculateCuttingList_EmptyRules_ReturnsEmpty()
    {
        // Arrange
        var item = CreateItem();
        var rule = CreateRule();
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, Array.Empty<PartDimensionRule>(), constants);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CalculateCuttingList_FiltersByDoorType()
    {
        // Arrange
        var item = CreateItem(DoorType.FAF_T);
        var rule = CreateRule("FAF_T");
        var matchingRule = CreateDimRule("FAF_T", "Component A");
        var nonMatchingRule = CreateDimRule("Falcos", "Component B"); // different door type
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { matchingRule, nonMatchingRule }, constants);

        // Assert
        result.Should().HaveCount(1);
        result[0].ComponentName.Should().Be("Component A");
    }

    [Fact]
    public void CalculateCuttingList_RoundsToOneDecimal()
    {
        // Arrange
        // doorW=850.3, bkmWidthFixed=0, widthMultiplier=1, oversize=1.1
        // width = 0 + 1*850.3 + 1.1 = 851.4 → rounds to 851.4
        var item = CreateItem(wallW: 860m, doorW: 850.3m);
        var rule = CreateRule(bkmWidthFixed: 0m, bkmHeightFixed: 0m);
        var dimRule = CreateDimRule(widthBase: 0m, widthMultiplier: 1m, lengthBase: 0m, lengthMultiplier: 0m);
        var constants = CreateOversize(1.1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result[0].Width.Should().Be(851.4m);
    }

    [Fact]
    public void CalculateFinishedDimensions_ReturnsCorrectCount()
    {
        // Arrange
        var item = CreateItem();
        var rule = CreateRule();
        var dimRules = new[]
        {
            CreateDimRule(componentName: "Part 1"),
            CreateDimRule(componentName: "Part 2"),
            CreateDimRule(componentName: "Part 3")
        };
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateFinishedDimensions(item, rule, dimRules, constants);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void CalculateCuttingList_IsDeterministic_SameInputSameOutput()
    {
        // Arrange
        var item = CreateItem();
        var rule = CreateRule();
        var dimRule = CreateDimRule();
        var constants = CreateOversize(1m);

        // Act
        var result1 = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);
        var result2 = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result1[0].Width.Should().Be(result2[0].Width);
        result1[0].Length.Should().Be(result2[0].Length);
        result1[0].Quantity.Should().Be(result2[0].Quantity);
    }

    [Fact]
    public void CalculateCuttingList_WidthMultiplierFactor_Applied()
    {
        // Arrange
        // DoorWidth=800, BkmWidthFixed=10 → bkmWidthFixed=810
        // WidthBase=5, WidthMultiplierFactor=2 → width = 5 + 2*810 + 1 = 1626
        var item = CreateItem(wallW: 900m, doorW: 800m);
        var rule = CreateRule(bkmWidthFixed: 10m, bkmHeightFixed: 0m);
        var dimRule = CreateDimRule(widthBase: 5m, widthMultiplier: 2m, lengthBase: 0m, lengthMultiplier: 0m);
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result[0].Width.Should().Be(1626m); // 5 + 2*(800+10) + 1
    }

    [Fact]
    public void CalculateCuttingList_LengthMultiplierFactor_Applied()
    {
        // Arrange
        // DoorHeight=2000, BkmHeightFixed=6 → bkmHeightFixed=2006
        // LengthBase=10, LengthMultiplierFactor=0.5 → length = 10 + 0.5*2006 + 1 = 1014
        var item = CreateItem(wallH: 2100m, doorH: 2000m);
        var rule = CreateRule(bkmWidthFixed: 0m, bkmHeightFixed: 6m);
        var dimRule = CreateDimRule(widthBase: 0m, widthMultiplier: 0m, lengthBase: 10m, lengthMultiplier: 0.5m);
        var constants = CreateOversize(1m);

        // Act
        var result = _sut.CalculateCuttingList(item, rule, new[] { dimRule }, constants);

        // Assert
        result[0].Length.Should().Be(1014m); // 10 + 0.5*(2000+6) + 1
    }
}
