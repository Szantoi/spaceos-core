namespace SpaceOS.Modules.Joinery.Domain.Results;

public sealed record FinishedDimension(
    string ItemSorszam,
    string ComponentName,
    decimal Width,
    decimal Length,
    int Quantity,
    string Material,
    string SurfaceType,
    string? Note);
