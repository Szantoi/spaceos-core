using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorOrderReverted(Guid OrderId, Guid TenantId) : IDomainEvent;
