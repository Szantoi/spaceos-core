using System.Text.Json;
using Xunit;
using FluentAssertions;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Contracts.Inventory.Enums;

namespace SpaceOS.Modules.Contracts.Tests.Inventory;

public class InventoryContractTests
{
    [Fact]
    public void StockMovementDto_ReferenceType_IsEnum()
    {
        var movement = new StockMovementDto(
            Guid.NewGuid(),
            StockMovementType.Consumed,
            5,
            StockReferenceType.CuttingSheet,
            Guid.NewGuid());

        movement.ReferenceType.GetType().Should().Be(typeof(StockReferenceType));
        movement.ReferenceType.Should().Be(StockReferenceType.CuttingSheet);
    }

    [Fact]
    public void ConsumptionTrendDto_EstimatedDaysUntilStockout_CanBeNull()
    {
        var trend = new ConsumptionTrendDto(
            MaterialCode: "MDF18",
            From: DateTimeOffset.UtcNow.AddDays(-30),
            To: DateTimeOffset.UtcNow,
            TotalConsumed: 50m,
            AverageDaily: 1.67m,
            EstimatedDaysUntilStockout: null);

        trend.EstimatedDaysUntilStockout.Should().BeNull();
    }

    [Fact]
    public void StockMovementType_Serialize_Roundtrip()
    {
        var original = StockMovementType.OffcutCreated;

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<StockMovementType>(json);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void MaterialCategory_AllValues_Correct()
    {
        var values = Enum.GetValues<MaterialCategory>();

        values.Should().Contain(MaterialCategory.Board);
        values.Should().Contain(MaterialCategory.Edge);
        values.Should().Contain(MaterialCategory.Veneer);
        values.Should().Contain(MaterialCategory.Hardware);
        values.Should().Contain(MaterialCategory.Adhesive);
        values.Should().Contain(MaterialCategory.Other);
        values.Should().HaveCount(6);
    }
}
