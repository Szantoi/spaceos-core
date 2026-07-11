using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorItemAdded(Guid OrderId, Guid TenantId, Guid ItemId) : IDomainEvent;
