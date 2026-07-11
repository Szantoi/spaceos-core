namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Aggregated result of the nesting optimization run for a cutting sheet.</summary>
public sealed record NestingResultDto(
    Guid SheetId,
    IReadOnlyList<PanelAssignmentDto> Assignments,
    decimal TotalWastePercentage,
    int PanelsUsed);
