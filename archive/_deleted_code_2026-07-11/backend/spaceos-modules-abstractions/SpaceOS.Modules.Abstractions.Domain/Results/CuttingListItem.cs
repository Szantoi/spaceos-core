namespace SpaceOS.Modules.Abstractions.Domain.Results;

public sealed record CuttingListItem(
    Guid SlotId, string SlotName, string ComponentType,
    decimal Width, decimal Height, decimal Depth,
    int Quantity, string? Material);
