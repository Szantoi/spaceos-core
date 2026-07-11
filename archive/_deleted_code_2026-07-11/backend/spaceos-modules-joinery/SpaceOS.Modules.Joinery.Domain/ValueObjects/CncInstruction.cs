namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single CNC machine instruction for a component.
/// </summary>
public sealed record CncInstruction(
    string ComponentName,
    string Operation,
    string? Position,
    decimal? Diameter,
    decimal? Depth,
    decimal? Angle,
    string? Note);
