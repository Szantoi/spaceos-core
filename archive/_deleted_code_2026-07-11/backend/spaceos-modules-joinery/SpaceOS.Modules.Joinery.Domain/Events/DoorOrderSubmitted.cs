using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorOrderSubmitted(Guid OrderId, Guid TenantId) : IDomainEvent;
