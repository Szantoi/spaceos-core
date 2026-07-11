using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

public sealed class DoorItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Sorszam { get; private set; } = string.Empty;
    public string? Name { get; private set; }
    public int Quantity { get; private set; }
    public DoorType DoorType { get; private set; }
    public OpeningDirection OpeningDirection { get; private set; }
    public DoorDimensions Dimensions { get; private set; } = null!;
    public SurfaceSpec? FixSide { get; private set; }
    public SurfaceSpec? MovingSide { get; private set; }
    public GlazingSpec? Glazing { get; private set; }
    public HardwareSpec? Hardware { get; private set; }
    public MaterialSpec? Materials { get; private set; }
    public ProcessingSpec? Processing { get; private set; }

    private DoorItem() { } // EF Core

    public static DoorItem Create(
        Guid orderId,
        string sorszam,
        int quantity,
        DoorType doorType,
        OpeningDirection dir,
        DoorDimensions dims)
    {
        return new DoorItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Sorszam = sorszam,
            Quantity = quantity,
            DoorType = doorType,
            OpeningDirection = dir,
            Dimensions = dims
        };
    }

    public void SetFixSide(SurfaceSpec? spec) => FixSide = spec;
    public void SetMovingSide(SurfaceSpec? spec) => MovingSide = spec;
    public void SetGlazing(GlazingSpec? spec) => Glazing = spec;
    public void SetHardware(HardwareSpec? spec) => Hardware = spec;
    public void SetMaterials(MaterialSpec? spec) => Materials = spec;
    public void SetProcessing(ProcessingSpec? spec) => Processing = spec;
    public void SetName(string? name) => Name = name;
}
