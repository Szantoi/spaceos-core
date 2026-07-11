// SpaceOS.Kernel.Application/Events/DomainEventDispatcher.cs

using MediatR;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Application.Events;

/// <summary>
/// MediatR-backed implementation of <see cref="IDomainEventDispatcher"/> that publishes
/// each domain event through the MediatR <see cref="IPublisher"/> pipeline and then records
/// an audit entry via <see cref="IAuditEventDispatcher"/>.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly IAuditEventDispatcher _auditEventDispatcher;

    /// <summary>
    /// Initialises a new <see cref="DomainEventDispatcher"/>.
    /// </summary>
    /// <param name="publisher">The MediatR publisher used to dispatch notifications.</param>
    /// <param name="auditEventDispatcher">The dispatcher that persists audit entries for each event.</param>
    public DomainEventDispatcher(IPublisher publisher, IAuditEventDispatcher auditEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(auditEventDispatcher);
        _publisher            = publisher;
        _auditEventDispatcher = auditEventDispatcher;
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var eventList = domainEvents.ToList();

        var tasks = eventList.Select(e => _publisher.Publish(e, cancellationToken)).ToList();
        await Task.WhenAll(tasks).ConfigureAwait(false);

        await _auditEventDispatcher
            .DispatchAsync(eventList.AsReadOnly(), cancellationToken)
            .ConfigureAwait(false);
    }
}
