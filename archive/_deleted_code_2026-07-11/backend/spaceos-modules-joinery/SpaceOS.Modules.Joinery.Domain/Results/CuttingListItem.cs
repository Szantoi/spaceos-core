namespace SpaceOS.Modules.Joinery.Domain.Results;

public sealed record CuttingListItem(
    string ItemSorszam,
    string ComponentName,
    string Material,
    decimal Thickness,
    decimal Width,
    decimal Length,
    int Quantity,
    string ComponentType);
