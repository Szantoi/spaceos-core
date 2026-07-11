using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorOrderCalculated(Guid OrderId, Guid TenantId, int ItemCount) : IDomainEvent;
