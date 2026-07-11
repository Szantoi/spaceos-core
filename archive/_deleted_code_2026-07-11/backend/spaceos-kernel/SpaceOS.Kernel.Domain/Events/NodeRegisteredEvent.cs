// SpaceOS.Kernel.Domain/Events/NodeRegisteredEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Federation.NodeManifest"/> is registered.
/// </summary>
public readonly record struct NodeRegisteredEvent(
    Guid NodeManifestId,
    TenantId TenantId,
    string ServerUrl,
    string ApiVersion,
    DateTimeOffset OccurredOn) : IDomainEvent;
