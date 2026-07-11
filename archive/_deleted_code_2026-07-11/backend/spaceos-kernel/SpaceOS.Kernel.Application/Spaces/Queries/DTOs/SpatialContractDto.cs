// SpaceOS.Kernel.Application/Spaces/Queries/DTOs/SpatialContractDto.cs

namespace SpaceOS.Kernel.Application.Spaces.Queries;

/// <summary>
/// Data transfer object representing a spatial element's contract at a point in time.
/// ElementType is INTENTIONALLY ABSENT per ADR-008 — it is driver-specific
/// and must never cross the server boundary.
/// </summary>
/// <param name="ElementId">The spatial element identifier.</param>
/// <param name="MinX">Minimum X coordinate in millimetres.</param>
/// <param name="MinY">Minimum Y coordinate in millimetres.</param>
/// <param name="MinZ">Minimum Z coordinate in millimetres.</param>
/// <param name="MaxX">Maximum X coordinate in millimetres.</param>
/// <param name="MaxY">Maximum Y coordinate in millimetres.</param>
/// <param name="MaxZ">Maximum Z coordinate in millimetres.</param>
/// <param name="TradeType">The construction trade type.</param>
/// <param name="FsmStateAtT">The FSM state of the element at the queried point in time.</param>
/// <param name="ReachedAt">The timestamp when the FSM state was reached.</param>
public sealed record SpatialContractDto(
    Guid ElementId,
    int MinX, int MinY, int MinZ,
    int MaxX, int MaxY, int MaxZ,
    string TradeType,
    string FsmStateAtT,
    DateTimeOffset ReachedAt);
