// SpaceOS.Kernel.Tests/Entities/FlowEpicCloseTests.cs

using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>Unit tests for <see cref="FlowEpic.Close"/> method and ClosedDone phase invariants.</summary>
public class FlowEpicCloseTests
{
    private const string ValidProofUrl  = "https://storage.example.com/proof/doc.pdf";
    private const string ValidProofHash = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5e6f7a8b9c0d1e2";

    private static FlowEpic CreateDeliveryEpic()
    {
        var epic = FlowEpic.Create("Delivery Epic", FacilityId.New(), TenantId.New());
        epic.StartExecution();
        epic.PopDomainEvents(); // clear creation + execution events
        return epic;
    }

    [Fact]
    public void Close_FromDeliveryPhase_TransitionsToClosedDone()
    {
        // Arrange
        var epic = CreateDeliveryEpic();

        // Act
        epic.Close(ValidProofUrl, ValidProofHash);

        // Assert
        Assert.Equal(WorkflowPhase.ClosedDone, epic.Phase);
    }

    [Fact]
    public void Close_FromDeliveryPhase_SetsProofUrl()
    {
        // Arrange
        var epic = CreateDeliveryEpic();

        // Act
        epic.Close(ValidProofUrl, ValidProofHash);

        // Assert
        Assert.Equal(ValidProofUrl, epic.ProofUrl);
    }

    [Fact]
    public void Close_FromDeliveryPhase_SetsProofHash()
    {
        // Arrange
        var epic = CreateDeliveryEpic();

        // Act
        epic.Close(ValidProofUrl, ValidProofHash);

        // Assert
        Assert.Equal(ValidProofHash, epic.ProofHash);
    }

    [Fact]
    public void Close_FromDeliveryPhase_RaisesFlowEpicClosedEvent()
    {
        // Arrange
        var epic = CreateDeliveryEpic();

        // Act
        epic.Close(ValidProofUrl, ValidProofHash);

        // Assert
        var events = epic.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<FlowEpicClosedEvent>(events[0]);
    }

    [Fact]
    public void Close_FromDeliveryPhase_ClosedEventContainsCorrectData()
    {
        // Arrange
        var epic = CreateDeliveryEpic();

        // Act
        epic.Close(ValidProofUrl, ValidProofHash);

        // Assert
        var events = epic.PopDomainEvents();
        var evt = (FlowEpicClosedEvent)events[0];
        Assert.Equal(epic.Id, evt.FlowEpicId);
        Assert.Equal(epic.TenantId, evt.TenantId);
        Assert.Equal(ValidProofHash, evt.ProofHash);
    }

    [Fact]
    public void Close_FromDiscoveryPhase_ThrowsDomainException()
    {
        // Arrange
        var epic = FlowEpic.Create("Discovery Epic", FacilityId.New(), TenantId.New());
        // Phase is Discovery — never called StartExecution()

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => epic.Close(ValidProofUrl, ValidProofHash));
        Assert.Contains("Delivery", ex.Message);
    }

    [Fact]
    public void Close_WhenAlreadyClosedDone_ThrowsDomainException()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        epic.Close(ValidProofUrl, ValidProofHash);
        epic.PopDomainEvents();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => epic.Close(ValidProofUrl, ValidProofHash));
        Assert.Contains("Delivery", ex.Message);
    }
}
