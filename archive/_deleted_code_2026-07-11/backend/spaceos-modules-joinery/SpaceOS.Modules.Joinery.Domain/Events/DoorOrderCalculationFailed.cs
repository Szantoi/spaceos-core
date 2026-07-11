using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorOrderCalculationFailed(Guid OrderId, Guid TenantId, string? Reason) : IDomainEvent;
