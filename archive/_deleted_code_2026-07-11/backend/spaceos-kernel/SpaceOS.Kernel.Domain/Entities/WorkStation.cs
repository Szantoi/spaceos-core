using System;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Aggregate root representing a workstation within a facility.
/// </summary>
public class WorkStation : AggregateRoot
{
    /// <summary>Gets the unique identifier of this workstation.</summary>
    public WorkStationId Id { get; init; }

    /// <summary>Gets the display name of this workstation.</summary>
    public WorkStationName Name { get; private set; }

    /// <summary>Gets the type classification of this workstation.</summary>
    public WorkStationType Type { get; private set; }

    /// <summary>Gets the identifier of the facility this workstation belongs to.</summary>
    public FacilityId FacilityId { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this workstation.</summary>
    public TenantId TenantId { get; init; }

    /// <summary>Gets the current operational status of this workstation.</summary>
    public WorkStationStatus Status { get; private set; }

    /// <summary>Gets a value indicating whether this workstation has been archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Initialises a <see cref="WorkStation"/> with an existing identity and attributes.
    /// The initial status is set to <see cref="WorkStationStatus.Available"/>.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="type">The type classification.</param>
    /// <param name="facilityId">The owning facility identifier.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    private WorkStation(WorkStationId id, WorkStationName name, WorkStationType type, FacilityId facilityId, TenantId tenantId)
    {
        Id = id;
        Name = name;
        Type = type;
        FacilityId = facilityId;
        TenantId = tenantId;
        Status = WorkStationStatus.Available;
    }

    /// <summary>
    /// Creates a new <see cref="WorkStation"/> with a freshly generated identifier.
    /// Raises a <see cref="WorkStationRegisteredEvent"/>.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <param name="type">The type classification string (validated by <see cref="WorkStationType"/>).</param>
    /// <param name="facilityId">The owning facility identifier.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <returns>A newly created <see cref="WorkStation"/> instance.</returns>
    public static WorkStation Create(string name, string type, FacilityId facilityId, TenantId tenantId)
    {
        var workStation = new WorkStation(
            WorkStationId.New(),
            WorkStationName.From(name),
            WorkStationType.From(type),
            facilityId,
            tenantId);
        workStation.AddDomainEvent(new WorkStationRegisteredEvent(workStation.Id, facilityId, DateTimeOffset.UtcNow));
        return workStation;
    }

    /// <summary>
    /// Changes the operational status of this workstation.
    /// Raises a <see cref="WorkStationStatusChangedEvent"/> when the status actually changes.
    /// </summary>
    /// <param name="newStatus">The desired new status.</param>
    /// <returns>The same <see cref="WorkStation"/> instance for fluent chaining.</returns>
    public WorkStation ChangeStatus(WorkStationStatus newStatus)
    {
        if (Status == newStatus) return this;

        var oldStatus = Status;
        Status = newStatus;

        AddDomainEvent(new WorkStationStatusChangedEvent(
            Id,
            oldStatus,
            newStatus,
            DateTimeOffset.UtcNow));

        return this;
    }

    /// <summary>
    /// Assigns this workstation to a different facility.
    /// Raises a <see cref="WorkStationReassignedEvent"/>.
    /// </summary>
    /// <param name="newFacilityId">The target facility identifier.</param>
    /// <returns>The same <see cref="WorkStation"/> instance for fluent chaining.</returns>
    public WorkStation AssignToFacility(FacilityId newFacilityId)
    {
        var oldFacilityId = FacilityId;
        FacilityId = newFacilityId;
        AddDomainEvent(new WorkStationReassignedEvent(Id, oldFacilityId, newFacilityId, DateTimeOffset.UtcNow));
        return this;
    }

    /// <summary>
    /// Updates the display name of this workstation.
    /// Raises a <see cref="WorkStationRenamedEvent"/>.
    /// </summary>
    /// <param name="newName">The new display name.</param>
    /// <returns>The same <see cref="WorkStation"/> instance for fluent chaining.</returns>
    public WorkStation UpdateName(string newName)
    {
        var oldName = Name.Value;
        Name = WorkStationName.From(newName);
        AddDomainEvent(new WorkStationRenamedEvent(Id, oldName, Name.Value, DateTimeOffset.UtcNow));
        return this;
    }

    /// <summary>Archives this workstation, preventing it from appearing in list results.</summary>
    /// <exception cref="DomainException">Thrown when the workstation is already archived.</exception>
    public void Archive()
    {
        if (IsArchived)
            throw new DomainException($"{nameof(WorkStation)} is already archived.");
        IsArchived = true;
        AddDomainEvent(new WorkStationArchivedEvent(Id, DateTimeOffset.UtcNow));
    }
}
