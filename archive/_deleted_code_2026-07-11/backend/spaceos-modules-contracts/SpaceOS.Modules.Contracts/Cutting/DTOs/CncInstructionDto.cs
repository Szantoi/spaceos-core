namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>
/// Represents a single CNC machining instruction associated with a component.
/// Operation: e.g. Bore, Groove, Route. Note: max 2000 chars.
/// </summary>
public sealed record CncInstructionDto(
    string ComponentName,
    string Operation,
    string? Position,
    decimal? Diameter,
    decimal? Depth,
    decimal? Angle,
    string? Note);
