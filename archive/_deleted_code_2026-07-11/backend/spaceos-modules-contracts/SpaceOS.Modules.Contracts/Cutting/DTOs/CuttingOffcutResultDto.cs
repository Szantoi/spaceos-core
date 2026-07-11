namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Represents a resulting offcut from a cutting operation (Cutting-side ownership).</summary>
public sealed record CuttingOffcutResultDto(
    string MaterialCode,
    decimal Width,
    decimal Height,
    decimal Thickness,
    Guid OriginSheetId);
