using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="SpaceOS.Kernel.Domain.Entities.Tenant"/> display name is updated.
/// </summary>
public readonly record struct TenantRenamedEvent(
    TenantId       TenantId,
    string         OldName,
    string         NewName,
    DateTimeOffset OccurredOn) : IDomainEvent;
