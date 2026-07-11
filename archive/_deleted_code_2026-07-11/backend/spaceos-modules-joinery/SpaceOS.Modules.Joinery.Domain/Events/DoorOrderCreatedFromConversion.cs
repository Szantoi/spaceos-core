using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Domain.Events;

public sealed record DoorOrderCreatedFromConversion(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    Guid SourceQuoteId) : IDomainEvent;
