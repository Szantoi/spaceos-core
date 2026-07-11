using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Dispatches a collection of domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Publishes each event in <paramref name="domainEvents"/> to its registered handlers.
    /// </summary>
    /// <param name="domainEvents">The events to dispatch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
