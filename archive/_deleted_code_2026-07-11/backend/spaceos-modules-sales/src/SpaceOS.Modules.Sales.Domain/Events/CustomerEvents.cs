using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Domain.Events;

public record CustomerRegistered(Guid CustomerId, Guid TenantId, CustomerType Type) : IDomainEvent;
public record CustomerUpdated(Guid CustomerId, Guid TenantId) : IDomainEvent;
public record CustomerLinkRequested(Guid CustomerId, Guid TenantId, Guid PlatformTenantId, LinkVerificationStatus Status) : IDomainEvent;
public record CustomerLinkVerified(Guid CustomerId, Guid TenantId, Guid PlatformTenantId) : IDomainEvent;
public record CustomerUnlinkedFromActor(Guid CustomerId, Guid TenantId) : IDomainEvent;
public record CustomerArchived(Guid CustomerId, Guid TenantId) : IDomainEvent;
