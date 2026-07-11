namespace SpaceOS.Modules.Joinery.Domain.Results;

public sealed record HardwareListItem(
    string ItemSorszam,
    string ComponentType,
    string Name,
    int Quantity,
    string Color,
    string? Note);
