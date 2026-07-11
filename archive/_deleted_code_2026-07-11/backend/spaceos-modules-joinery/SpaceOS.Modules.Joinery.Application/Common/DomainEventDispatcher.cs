using MediatR;
using SpaceOS.Modules.Joinery.Domain.Common;

namespace SpaceOS.Modules.Joinery.Application.Common;

public static class DomainEventDispatcher
{
    public static async Task DispatchAsync(IMediator mediator, IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var evt in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(evt.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, evt)!;
            await mediator.Publish(notification, ct).ConfigureAwait(false);
        }
    }
}
