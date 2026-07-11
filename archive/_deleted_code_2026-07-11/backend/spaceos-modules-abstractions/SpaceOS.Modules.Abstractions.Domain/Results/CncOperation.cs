using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Domain.Results;

public sealed record CncOperation(
    Guid SlotId, string SlotName,
    MachiningOperation Operation,
    decimal? GrooveDepth, decimal? GrooveWidth,
    decimal? DrillDiameter, decimal? DrillDepth,
    decimal? Angle, decimal? Radius,
    string? Note);
