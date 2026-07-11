// SpaceOS.Kernel.Domain/Events/TenantModulesUpdatedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when the enabled module list of a <see cref="SpaceOS.Kernel.Domain.Entities.Tenant"/>
/// is updated via <c>UpdateEnabledModules</c>.
/// </summary>
public readonly record struct TenantModulesUpdatedEvent(
    TenantId       TenantId,
    string[]       EnabledModules,
    DateTimeOffset OccurredOn) : IDomainEvent;
