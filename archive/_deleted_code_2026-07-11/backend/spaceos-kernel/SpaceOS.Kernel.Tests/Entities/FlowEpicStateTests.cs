using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

public class FlowEpicStateTests
{
    [Fact]
    public void Create_ShouldRaiseFlowEpicCreatedEvent()
    {
        // Arrange & Act
        var facilityId = FacilityId.New();
        var epic = FlowEpic.Create("Epic 1", facilityId, TenantId.New());

        // Assert
        var events = epic.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<FlowEpicCreatedEvent>(events[0]);
        var evt = (FlowEpicCreatedEvent)events[0];
        Assert.Equal(epic.Id, evt.FlowEpicId);
        Assert.Equal(facilityId, evt.TargetFacilityId);
    }

    [Fact]
    public void Create_ShouldStartInDiscoveryPhase()
    {
        // Arrange & Act
        var flowEpic = FlowEpic.Create("Kitchen Manufacturing", FacilityId.New(), TenantId.New());

        // Assert
        Assert.Equal(WorkflowPhase.Discovery, flowEpic.Phase);
    }

    [Fact]
    public void StartExecution_FromDiscovery_ShouldTransitionToDeliveryPhase()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Kitchen Manufacturing", FacilityId.New(), TenantId.New());

        // Act
        flowEpic.StartExecution();

        // Assert
        Assert.Equal(WorkflowPhase.Delivery, flowEpic.Phase);
    }

    [Fact]
    public void StartExecution_WhenAlreadyInDelivery_ShouldThrowDomainException()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Kitchen Manufacturing", FacilityId.New(), TenantId.New());
        flowEpic.StartExecution();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => flowEpic.StartExecution());
        Assert.Contains("already in Delivery", exception.Message);
    }

    [Fact]
    public void UpdateTitle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var epic = FlowEpic.Create("Original Title", FacilityId.New(), TenantId.New());
        epic.PopDomainEvents(); // clear creation event

        // Act
        epic.UpdateTitle("New Title");

        // Assert
        var events = epic.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<FlowEpicTitleUpdatedEvent>(events[0]);
        var evt = (FlowEpicTitleUpdatedEvent)events[0];
        Assert.Equal("Original Title", evt.OldTitle);
        Assert.Equal("New Title", evt.NewTitle);
    }
}
