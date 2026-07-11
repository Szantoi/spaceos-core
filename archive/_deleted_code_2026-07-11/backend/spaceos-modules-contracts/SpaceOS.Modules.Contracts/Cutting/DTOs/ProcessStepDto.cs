namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Represents a single production process step attached to a cutting sheet.</summary>
public sealed record ProcessStepDto(
    string Phase,
    int StepOrder,
    string Description,
    int EstimatedSeconds);
