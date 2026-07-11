// SpaceOS.Kernel.Application/Spaces/Queries/DTOs/SpatialTimelineEventDto.cs

namespace SpaceOS.Kernel.Application.Spaces.Queries;

/// <summary>
/// Data transfer object representing a single spatial timeline event —
/// a FlowTask state transition joined with its spatial element.
/// </summary>
/// <param name="OccurredAt">The timestamp of the state change.</param>
/// <param name="ElementId">The spatial element identifier.</param>
/// <param name="TradeType">The construction trade type.</param>
/// <param name="FromState">The previous FSM state.</param>
/// <param name="ToState">The new FSM state.</param>
public sealed record SpatialTimelineEventDto(
    DateTimeOffset OccurredAt,
    Guid ElementId,
    string TradeType,
    string FromState,
    string ToState);
