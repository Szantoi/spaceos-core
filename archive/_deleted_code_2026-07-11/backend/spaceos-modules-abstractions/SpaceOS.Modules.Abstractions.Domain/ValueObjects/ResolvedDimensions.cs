namespace SpaceOS.Modules.Abstractions.Domain.ValueObjects;

public sealed record ResolvedDimensions(decimal Width, decimal Height, decimal Depth)
{
    public static ResolvedDimensions FromInput(DimensionInput i) => new(i.Width, i.Height, i.Depth);
}
