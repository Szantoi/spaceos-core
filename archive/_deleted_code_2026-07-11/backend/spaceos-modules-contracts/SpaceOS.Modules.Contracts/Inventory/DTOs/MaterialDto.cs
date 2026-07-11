using SpaceOS.Modules.Contracts.Inventory.Enums;

namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>
/// Master data record describing a material type in the system.
/// Code: max 20 chars. Unit: e.g. m2, pcs, m. Null if not applicable.
/// </summary>
public sealed record MaterialDto(
    string Code,
    string Name,
    MaterialCategory Category,
    decimal StandardWidth,
    decimal StandardHeight,
    decimal Thickness,
    string? Unit);
