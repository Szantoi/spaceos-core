namespace SpaceOS.Modules.Abstractions.Domain.Common;

public abstract class TenantScopedEntity
{
    public Guid Id { get; protected set; }
    public Guid TenantId { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}

public interface IDomainEvent { }

public sealed class DomainException : Exception
{
    public DomainException(string msg) : base(msg) { }
}
