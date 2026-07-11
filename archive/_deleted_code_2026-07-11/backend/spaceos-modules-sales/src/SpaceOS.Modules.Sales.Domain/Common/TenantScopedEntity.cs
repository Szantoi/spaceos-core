namespace SpaceOS.Modules.Sales.Domain.Common;

/// <summary>
/// Base class for all tenant-scoped aggregates. Provides identity, tenant ownership
/// and domain event collection/dispatch support.
/// </summary>
public abstract class TenantScopedEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Aggregate identifier.</summary>
    public Guid Id { get; protected set; }

    /// <summary>The tenant that owns this entity.</summary>
    public Guid TenantId { get; protected set; }

    /// <summary>Snapshot of pending domain events. Read-only outside the aggregate.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Adds a domain event to the pending queue.</summary>
    protected void AddDomainEvent(IDomainEvent evt) => _domainEvents.Add(evt);

    /// <summary>
    /// Returns all pending domain events and clears the internal list.
    /// Called by infrastructure after SaveChanges to dispatch events.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>Clears all pending domain events without returning them.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Adds an event derived from this entity and returns <c>this</c> for fluent use in static factories.
    /// </summary>
    protected TenantScopedEntity WithEvent(Func<TenantScopedEntity, IDomainEvent> evt)
    {
        AddDomainEvent(evt(this));
        return this;
    }
}
