using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Domain.Events;

public record QuoteCreated(Guid QuoteId, Guid TenantId, Guid CustomerId, QuoteNumber Number) : IDomainEvent;
public record QuoteSent(Guid QuoteId, Guid TenantId, Guid CustomerId) : IDomainEvent;
public record QuoteAccepted(Guid QuoteId, Guid TenantId, Guid CustomerId) : IDomainEvent;
public record QuoteRejected(Guid QuoteId, Guid TenantId, string Reason) : IDomainEvent;
public record QuoteConversionRequested(Guid QuoteId, Guid TenantId, Guid CustomerId) : IDomainEvent;
public record QuoteConverted(Guid QuoteId, Guid TenantId, Guid CustomerId, Guid OrderId) : IDomainEvent;
public record QuoteConversionFailed(Guid QuoteId, Guid TenantId, string Reason) : IDomainEvent;
public record QuoteArchived(Guid QuoteId, Guid TenantId) : IDomainEvent;
