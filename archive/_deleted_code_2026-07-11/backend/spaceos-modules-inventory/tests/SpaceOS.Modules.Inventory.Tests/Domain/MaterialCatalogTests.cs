using FluentAssertions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Domain;

public class MaterialCatalogTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var catalog = MaterialCatalog.Create("MDF 18mm", 2800, 2070, 18, 8500, "MDF-18", "MDF lap");
        catalog.MaterialType.Should().Be("MDF 18mm");
        catalog.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyMaterialType_ShouldThrow()
    {
        var act = () => MaterialCatalog.Create("", 2800, 2070, 18, 8500, "MDF-18", "MDF lap");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroWidth_ShouldThrow()
    {
        var act = () => MaterialCatalog.Create("MDF 18mm", 0, 2070, 18, 8500, "MDF-18", "MDF lap");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeThickness_ShouldThrow()
    {
        var act = () => MaterialCatalog.Create("MDF 18mm", 2800, 2070, -1, 8500, "MDF-18", "MDF lap");
        act.Should().Throw<ArgumentException>();
    }
}
