using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorOrderCreated(Guid OrderId, Guid TenantId, string ProjectId) : IDomainEvent;
