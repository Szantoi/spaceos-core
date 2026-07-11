using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Aggregate root representing a physical or logical facility belonging to a tenant.
/// </summary>
public class Facility : AggregateRoot
{
    /// <summary>Gets the unique identifier of this facility.</summary>
    public FacilityId Id { get; init; }

    /// <summary>Gets the display name of this facility.</summary>
    public FacilityName Name { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this facility.</summary>
    public TenantId TenantId { get; init; }

    /// <summary>Gets a value indicating whether this facility has been archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Initialises a <see cref="Facility"/> with an existing identity, name and tenant.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The facility name.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    private Facility(FacilityId id, FacilityName name, TenantId tenantId)
    {
        Id = id;
        Name = name;
        TenantId = tenantId;
    }

    /// <summary>
    /// Creates a new <see cref="Facility"/> with a freshly generated identifier.
    /// Raises a <see cref="FacilityCreatedEvent"/>.
    /// </summary>
    /// <param name="name">The display name for the facility.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <returns>A newly created <see cref="Facility"/> instance.</returns>
    public static Facility Create(string name, TenantId tenantId)
    {
        var facility = new Facility(FacilityId.New(), FacilityName.From(name), tenantId);
        facility.AddDomainEvent(new FacilityCreatedEvent(facility.Id, tenantId, DateTimeOffset.UtcNow));
        return facility;
    }

    /// <summary>
    /// Renames this facility.
    /// Raises a <see cref="FacilityRenamedEvent"/>.
    /// </summary>
    /// <param name="newName">The new display name.</param>
    /// <returns>The same <see cref="Facility"/> instance for fluent chaining.</returns>
    public Facility Rename(string newName)
    {
        var oldName = Name.Value;
        Name = FacilityName.From(newName);
        AddDomainEvent(new FacilityRenamedEvent(Id, oldName, Name.Value, DateTimeOffset.UtcNow));
        return this;
    }

    /// <summary>Archives this facility, preventing it from appearing in list results.</summary>
    /// <exception cref="DomainException">Thrown when the facility is already archived.</exception>
    public void Archive()
    {
        if (IsArchived)
            throw new DomainException($"{nameof(Facility)} is already archived.");
        IsArchived = true;
        AddDomainEvent(new FacilityArchivedEvent(Id, DateTimeOffset.UtcNow));
    }
}
