using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when the intent data of a local <see cref="SpaceOS.Kernel.Domain.Entities.SpaceLayer"/> is updated.
/// </summary>
/// <param name="SpaceLayerId">The identifier of the SpaceLayer whose intent data was updated.</param>
/// <param name="NewHash">The SHA-256 hash of the new intent data.</param>
/// <param name="OccurredOn">The UTC timestamp at which the update occurred.</param>
public readonly record struct SpaceLayerIntentUpdatedEvent(
    SpaceLayerId SpaceLayerId,
    string NewHash,
    DateTimeOffset OccurredOn) : IDomainEvent;
