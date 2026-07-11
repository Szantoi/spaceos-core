namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single production process step within a cutting list snapshot.
/// </summary>
public sealed record ProcessStep(
    string Phase,
    int StepOrder,
    string? Description,
    int EstimatedSeconds);
