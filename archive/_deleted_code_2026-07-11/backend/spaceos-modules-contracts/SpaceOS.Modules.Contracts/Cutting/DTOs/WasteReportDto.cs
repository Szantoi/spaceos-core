namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Aggregated waste report for a given time window across all cutting operations.</summary>
public sealed record WasteReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    decimal TotalMaterialUsedM2,
    decimal TotalWasteM2,
    decimal WastePercentage,
    IReadOnlyList<WasteByMaterialDto> ByMaterial);
