using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

public class WorkStationTests
{
    [Fact]
    public void Create_ShouldRaiseWorkStationRegisteredEvent()
    {
        // Arrange & Act
        var facilityId = FacilityId.New();
        var workStation = WorkStation.Create("WS-01", "Assembly", facilityId, TenantId.New());

        // Assert
        var events = workStation.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<WorkStationRegisteredEvent>(events[0]);
        var evt = (WorkStationRegisteredEvent)events[0];
        Assert.Equal(workStation.Id, evt.WorkStationId);
        Assert.Equal(facilityId, evt.FacilityId);
    }

    [Fact]
    public void ChangeStatus_ShouldRaiseWorkStationStatusChangedEvent()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var workStation = WorkStation.Create("WS-01", "Desk", facilityId, TenantId.New());
        workStation.PopDomainEvents(); // clear creation event
        var newStatus = WorkStationStatus.Occupied;

        // Act
        workStation.ChangeStatus(newStatus);

        // Assert
        var domainEvents = workStation.GetDomainEvents();
        Assert.Single(domainEvents);

        var @event = Assert.IsType<WorkStationStatusChangedEvent>(domainEvents.First());
        Assert.Equal(workStation.Id, @event.WorkStationId);
        Assert.Equal(WorkStationStatus.Available, @event.OldStatus);
        Assert.Equal(newStatus, @event.NewStatus);
        Assert.True(@event.OccurredOn <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void PopDomainEvents_ShouldReturnEventsAndClearInternalList()
    {
        // Arrange
        var workStation = WorkStation.Create("WS-01", "Desk", FacilityId.New(), TenantId.New());
        workStation.PopDomainEvents(); // clear creation event
        workStation.ChangeStatus(WorkStationStatus.Occupied);
        Assert.NotEmpty(workStation.GetDomainEvents());

        // Act
        var popped = workStation.PopDomainEvents();

        // Assert
        Assert.NotEmpty(popped);
        Assert.Empty(workStation.GetDomainEvents());
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_ShouldNotRaiseEvent()
    {
        // Arrange
        var workStation = WorkStation.Create("WS-01", "Desk", FacilityId.New(), TenantId.New());
        workStation.PopDomainEvents(); // clear creation event

        // Current status is Available. Change to Available should do nothing.

        // Act
        workStation.ChangeStatus(WorkStationStatus.Available);

        // Assert
        var domainEvents = workStation.GetDomainEvents();
        Assert.Empty(domainEvents);
    }

    [Fact]
    public void AssignToFacility_ShouldRaiseDomainEvent()
    {
        // Arrange
        var workStation = WorkStation.Create("Test WS", "Assembly", FacilityId.New(), TenantId.New());
        workStation.PopDomainEvents(); // clear creation event
        var newFacilityId = FacilityId.New();

        // Act
        workStation.AssignToFacility(newFacilityId);

        // Assert
        var events = workStation.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<WorkStationReassignedEvent>(events[0]);
        var evt = (WorkStationReassignedEvent)events[0];
        Assert.Equal(newFacilityId, evt.NewFacilityId);
    }

    [Fact]
    public void UpdateName_ShouldRaiseDomainEvent()
    {
        // Arrange
        var workStation = WorkStation.Create("Original Name", "Assembly", FacilityId.New(), TenantId.New());
        workStation.PopDomainEvents(); // clear creation event

        // Act
        workStation.UpdateName("New Name");

        // Assert
        var events = workStation.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<WorkStationRenamedEvent>(events[0]);
        var evt = (WorkStationRenamedEvent)events[0];
        Assert.Equal("Original Name", evt.OldName);
        Assert.Equal("New Name", evt.NewName);
    }
}
