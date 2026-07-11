namespace SpaceOS.Modules.Joinery.Domain.Results;

public sealed record QuantitySummary(
    int DoorLeafs,
    int FrameCores,
    int Cladding,
    int Blende,
    int FurnitureFront,
    int WallPanel,
    int Painted,
    int Foiled);
