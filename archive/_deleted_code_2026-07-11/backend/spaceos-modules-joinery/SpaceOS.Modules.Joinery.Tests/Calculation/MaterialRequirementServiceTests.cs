using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Infrastructure.Services;

namespace SpaceOS.Modules.Joinery.Tests.Calculation;

public class MaterialRequirementServiceTests
{
    private readonly MaterialRequirementService _sut = new();

    [Fact]
    public void Calculate_EmptyList_ReturnsEmpty()
    {
        var result = _sut.Calculate([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ReturnsReadOnlyList()
    {
        var result = _sut.Calculate([]);

        result.Should().BeAssignableTo<IReadOnlyList<MaterialRequirement>>();
    }

    [Fact]
    public void Calculate_WithItems_DoesNotThrow()
    {
        var items = new List<CuttingListItem>
        {
            new("A01", "Keret", "MDF", 18m, 850m, 2050m, 2, "Frame"),
            new("A01", "Betét", "MDF", 6m, 800m, 2000m, 1, "Insert")
        };

        var act = () => _sut.Calculate(items.AsReadOnly());

        act.Should().NotThrow();
    }
}
