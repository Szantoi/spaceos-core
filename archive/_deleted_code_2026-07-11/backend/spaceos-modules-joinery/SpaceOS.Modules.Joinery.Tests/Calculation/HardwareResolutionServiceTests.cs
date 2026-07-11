using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Services;

namespace SpaceOS.Modules.Joinery.Tests.Calculation;

public class HardwareResolutionServiceTests
{
    private readonly HardwareResolutionService _sut = new();

    private static DoorItem MakeItem()
    {
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        return DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
    }

    private static DoorTypeRule MakeRule() => new()
    {
        DoorType = "FAF_T",
        BkmWidthFixed = 8m,
        BkmHeightFixed = 4m
    };

    [Fact]
    public void Resolve_ReturnsReadOnlyList()
    {
        var result = _sut.Resolve(MakeItem(), MakeRule());

        result.Should().BeAssignableTo<IReadOnlyList<HardwareListItem>>();
    }

    [Fact]
    public void Resolve_DoesNotThrow()
    {
        var act = () => _sut.Resolve(MakeItem(), MakeRule());

        act.Should().NotThrow();
    }

    [Fact]
    public void Resolve_WithAnyInput_ReturnsEmpty()
    {
        var result = _sut.Resolve(MakeItem(), MakeRule());

        result.Should().BeEmpty();
    }
}
