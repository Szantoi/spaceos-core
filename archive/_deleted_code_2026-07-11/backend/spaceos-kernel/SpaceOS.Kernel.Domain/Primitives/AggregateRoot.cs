using System.Collections.Generic;

namespace SpaceOS.Kernel.Domain.Primitives;

/// <summary>
/// Base abstract class for Aggregate Roots that track and manage Domain Events.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets a read-only list of domain events collected by the aggregate.
    /// </summary>
    internal IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the internal collection.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Atomically returns all collected domain events and clears the internal list.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList().AsReadOnly();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>
    /// Clears all collected domain events (for testing).
    /// </summary>
    internal void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

}
