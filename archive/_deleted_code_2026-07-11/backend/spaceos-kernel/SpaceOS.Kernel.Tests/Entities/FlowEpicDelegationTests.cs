using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Events;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

public class FlowEpicDelegationTests
{
    [Fact]
    public void DelegateTo_ShouldSucceed_WhenInDiscoveryPhase()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Kitchen Manufacturing", FacilityId.New(), TenantId.New());
        var guestTenantId = TenantId.New();

        // Act
        flowEpic.DelegateTo(guestTenantId);

        // Assert
        Assert.NotNull(flowEpic.Handshake);
        Assert.Equal(guestTenantId, flowEpic.Handshake!.GuestTenantId);
        Assert.True(DateTimeOffset.UtcNow >= flowEpic.Handshake.DelegatedOn);
    }

    [Fact]
    public void DelegateTo_ShouldThrowDomainException_WhenInDeliveryPhase()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Kitchen Manufacturing", FacilityId.New(), TenantId.New());
        flowEpic.StartExecution();
        var guestTenantId = TenantId.New();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => flowEpic.DelegateTo(guestTenantId));
        Assert.Contains("Discovery phase", exception.Message);
    }

    [Fact]
    public void DelegateTo_ShouldRaiseFlowEpicDelegatedEvent_WhenSuccessful()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Kitchen Manufacturing", FacilityId.New(), TenantId.New());
        flowEpic.PopDomainEvents(); // clear creation event
        var guestTenantId = TenantId.New();

        // Act
        flowEpic.DelegateTo(guestTenantId);

        // Assert
        var domainEvents = flowEpic.PopDomainEvents();
        Assert.Single(domainEvents);
        var delegatedEvent = Assert.IsType<FlowEpicDelegatedEvent>(domainEvents[0]);
        Assert.Equal(flowEpic.Id, delegatedEvent.FlowEpicId);
        Assert.Equal(guestTenantId, delegatedEvent.GuestTenantId);
        Assert.NotEqual(default, delegatedEvent.OccurredOn);
    }
}
