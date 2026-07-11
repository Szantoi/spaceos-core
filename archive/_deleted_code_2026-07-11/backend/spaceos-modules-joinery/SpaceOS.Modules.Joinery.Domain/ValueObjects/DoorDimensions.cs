using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record DoorDimensions
{
    public decimal WallOpeningWidth { get; init; }
    public decimal DoorWidth { get; init; }
    public decimal WallOpeningHeight { get; init; }
    public decimal DoorHeight { get; init; }
    public decimal WallOpeningThickness { get; init; }
    public decimal DoorThickness { get; init; }

    private DoorDimensions() { }

    public static Result<DoorDimensions> Create(
        decimal wallOpeningWidth,
        decimal doorWidth,
        decimal wallOpeningHeight,
        decimal doorHeight,
        decimal wallOpeningThickness,
        decimal doorThickness)
    {
        if (doorWidth <= 0)
            return Result<DoorDimensions>.Invalid(new ValidationError("DoorWidth", "DoorWidth must be greater than 0."));
        if (doorHeight <= 0)
            return Result<DoorDimensions>.Invalid(new ValidationError("DoorHeight", "DoorHeight must be greater than 0."));
        if (doorWidth > wallOpeningWidth)
            return Result<DoorDimensions>.Invalid(new ValidationError("DoorWidth", "DoorWidth cannot exceed WallOpeningWidth."));
        if (doorHeight > wallOpeningHeight)
            return Result<DoorDimensions>.Invalid(new ValidationError("DoorHeight", "DoorHeight cannot exceed WallOpeningHeight."));
        if (doorWidth > 2600m)
            return Result<DoorDimensions>.Invalid(new ValidationError("DoorWidth", "DoorWidth exceeds maximum press width of 2600 mm."));
        if (doorHeight > 3000m)
            return Result<DoorDimensions>.Invalid(new ValidationError("DoorHeight", "DoorHeight exceeds maximum press height of 3000 mm."));

        return Result<DoorDimensions>.Success(new DoorDimensions
        {
            WallOpeningWidth = wallOpeningWidth,
            DoorWidth = doorWidth,
            WallOpeningHeight = wallOpeningHeight,
            DoorHeight = doorHeight,
            WallOpeningThickness = wallOpeningThickness,
            DoorThickness = doorThickness
        });
    }
}
