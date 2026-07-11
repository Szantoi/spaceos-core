namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Waste breakdown for a single material within a waste report.</summary>
public sealed record WasteByMaterialDto(
    string MaterialCode,
    decimal UsedM2,
    decimal WasteM2);
