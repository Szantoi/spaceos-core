using MediatR;
using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Application.Common;

public sealed class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
