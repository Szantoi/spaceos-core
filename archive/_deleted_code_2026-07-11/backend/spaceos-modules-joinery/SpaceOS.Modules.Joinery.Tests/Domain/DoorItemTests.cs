using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

public class DoorItemTests
{
    private static DoorDimensions ValidDims()
        => DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;

    [Fact]
    public void Create_SetsAllProperties()
    {
        var orderId = Guid.NewGuid();
        var item = DoorItem.Create(orderId, "B01", 3, DoorType.FAF_T, OpeningDirection.Right, ValidDims());

        item.OrderId.Should().Be(orderId);
        item.Sorszam.Should().Be("B01");
        item.Quantity.Should().Be(3);
        item.DoorType.Should().Be(DoorType.FAF_T);
        item.OpeningDirection.Should().Be(OpeningDirection.Right);
    }

    [Fact]
    public void Create_HasNewGuidId()
    {
        var item = DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());

        item.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TwoItems_HaveDifferentIds()
    {
        var orderId = Guid.NewGuid();
        var item1 = DoorItem.Create(orderId, "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());
        var item2 = DoorItem.Create(orderId, "A02", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());

        item1.Id.Should().NotBe(item2.Id);
    }

    [Fact]
    public void SetName_SetsName()
    {
        var item = DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());

        item.SetName("Bejárati ajtó");

        item.Name.Should().Be("Bejárati ajtó");
    }

    [Fact]
    public void SetFixSide_SetsFixSide()
    {
        var item = DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());
        var spec = SurfaceSpec.Create(SurfaceType.Painted, "RAL9010", null, null, null, null, null, false, false).Value;

        item.SetFixSide(spec);

        item.FixSide.Should().Be(spec);
    }

    [Fact]
    public void SetMovingSide_SetsMovingSide()
    {
        var item = DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());
        var spec = SurfaceSpec.Create(SurfaceType.Foiled, "Tölgy", null, null, null, null, null, false, false).Value;

        item.SetMovingSide(spec);

        item.MovingSide.Should().Be(spec);
    }

    [Fact]
    public void SetGlazing_SetsGlazing()
    {
        var item = DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());
        var glazing = GlazingSpec.Create("Edzett üveg", null, null, null).Value;

        item.SetGlazing(glazing);

        item.Glazing.Should().Be(glazing);
    }

    [Fact]
    public void SetProcessing_SetsProcessing()
    {
        var item = DoorItem.Create(Guid.NewGuid(), "A01", 1, DoorType.FAF_T, OpeningDirection.Left, ValidDims());
        var processing = ProcessingSpec.Create("CNC-001", null, null, null).Value;

        item.SetProcessing(processing);

        item.Processing.Should().Be(processing);
    }
}
