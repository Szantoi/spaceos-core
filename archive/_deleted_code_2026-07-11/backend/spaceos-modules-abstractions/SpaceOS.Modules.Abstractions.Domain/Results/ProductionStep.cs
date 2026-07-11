using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Domain.Results;

public sealed record ProductionStep(
    Guid SlotId, string SlotName,
    ProcessPhase Phase, int Order,
    JointType JointType, string? Note);
