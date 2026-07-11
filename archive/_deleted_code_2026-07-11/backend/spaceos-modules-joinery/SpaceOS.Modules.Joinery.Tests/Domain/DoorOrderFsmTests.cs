using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

/// <summary>
/// Unit tests for <see cref="DoorOrder"/> FSM transitions:
/// MarkCalculating, MarkCalculated, MarkCalculationFailed, RevertToDraft, and Version invariants.
/// </summary>
public class DoorOrderFsmTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static DoorOrder CreateDraftWithItem()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-FSM", "FSM Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "F01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        return order;
    }

    private static DoorOrder CreateSubmittedOrder()
    {
        var order = CreateDraftWithItem();
        order.Submit();
        return order;
    }

    private static DoorOrder CreateCalculatingOrder()
    {
        var order = CreateSubmittedOrder();
        order.MarkCalculating();
        return order;
    }

    private static DoorOrder CreateCalculatedOrder()
    {
        var order = CreateCalculatingOrder();
        order.MarkCalculated();
        return order;
    }

    private static DoorOrder CreateCalculationFailedOrder()
    {
        var order = CreateCalculatingOrder();
        order.MarkCalculationFailed("engine error");
        return order;
    }

    // --- MarkCalculating ---

    [Fact]
    public void MarkCalculating_WhenSubmitted_Succeeds()
    {
        var order = CreateSubmittedOrder();

        var result = order.MarkCalculating();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.Calculating);
    }

    [Fact]
    public void MarkCalculating_WhenSubmitted_IncrementsVersion()
    {
        var order = CreateSubmittedOrder();
        var versionBefore = order.Version;

        order.MarkCalculating();

        order.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void MarkCalculating_WhenDraft_ReturnsInvalid()
    {
        var order = CreateDraftWithItem();

        var result = order.MarkCalculating();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void MarkCalculating_WhenAlreadyCalculating_ReturnsInvalid()
    {
        var order = CreateCalculatingOrder();

        var result = order.MarkCalculating();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    // --- MarkCalculated ---

    [Fact]
    public void MarkCalculated_WhenCalculating_Succeeds()
    {
        var order = CreateCalculatingOrder();

        var result = order.MarkCalculated();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.Calculated);
    }

    [Fact]
    public void MarkCalculated_WhenCalculating_IncrementsVersion()
    {
        var order = CreateCalculatingOrder();
        var versionBefore = order.Version;

        order.MarkCalculated();

        order.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void MarkCalculated_WhenCalculating_RaisesOrderCalculatedEvent()
    {
        var order = CreateCalculatingOrder();
        order.PopDomainEvents();

        order.MarkCalculated();

        order.DomainEvents.Should().ContainSingle(e => e is DoorOrderCalculated);
        var evt = (DoorOrderCalculated)order.DomainEvents.First();
        evt.OrderId.Should().Be(order.Id);
        evt.TenantId.Should().Be(TenantId);
        evt.ItemCount.Should().Be(order.Items.Count);
    }

    [Fact]
    public void MarkCalculated_WhenNotCalculating_ReturnsInvalid()
    {
        var order = CreateSubmittedOrder();

        var result = order.MarkCalculated();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    // --- MarkCalculationFailed ---

    [Fact]
    public void MarkCalculationFailed_WhenCalculating_Succeeds()
    {
        var order = CreateCalculatingOrder();

        var result = order.MarkCalculationFailed("graph engine timeout");

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.CalculationFailed);
    }

    [Fact]
    public void MarkCalculationFailed_WhenCalculating_IncrementsVersion()
    {
        var order = CreateCalculatingOrder();
        var versionBefore = order.Version;

        order.MarkCalculationFailed("error");

        order.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void MarkCalculationFailed_WhenCalculating_StoresReason()
    {
        var order = CreateCalculatingOrder();

        order.MarkCalculationFailed("specific failure reason");

        order.CalculationError.Should().Be("specific failure reason");
    }

    [Fact]
    public void MarkCalculationFailed_ReasonExceeds2000Chars_TruncatesToMaxLength()
    {
        var order = CreateCalculatingOrder();
        var longReason = new string('x', 2500);

        order.MarkCalculationFailed(longReason);

        order.CalculationError.Should().HaveLength(2000);
    }

    [Fact]
    public void MarkCalculationFailed_WhenCalculating_RaisesCalculationFailedEvent()
    {
        var order = CreateCalculatingOrder();
        order.PopDomainEvents();

        order.MarkCalculationFailed("timeout");

        order.DomainEvents.Should().ContainSingle(e => e is DoorOrderCalculationFailed);
        var evt = (DoorOrderCalculationFailed)order.DomainEvents.First();
        evt.OrderId.Should().Be(order.Id);
        evt.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void MarkCalculationFailed_WhenNotCalculating_ReturnsInvalid()
    {
        var order = CreateSubmittedOrder();

        var result = order.MarkCalculationFailed("error");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    // --- RevertToDraft ---

    [Fact]
    public void RevertToDraft_FromCalculated_Succeeds()
    {
        var order = CreateCalculatedOrder();

        var result = order.RevertToDraft();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.Draft);
    }

    [Fact]
    public void RevertToDraft_FromCalculationFailed_Succeeds()
    {
        var order = CreateCalculationFailedOrder();

        var result = order.RevertToDraft();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.Draft);
    }

    [Fact]
    public void RevertToDraft_ClearsCalculationError()
    {
        var order = CreateCalculationFailedOrder();
        order.CalculationError.Should().NotBeNull();

        order.RevertToDraft();

        order.CalculationError.Should().BeNull();
    }

    [Fact]
    public void RevertToDraft_IncrementsVersion()
    {
        var order = CreateCalculatedOrder();
        var versionBefore = order.Version;

        order.RevertToDraft();

        order.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void RevertToDraft_WhenCalculated_RaisesOrderRevertedEvent()
    {
        var order = CreateCalculatedOrder();
        order.PopDomainEvents();

        order.RevertToDraft();

        order.DomainEvents.Should().ContainSingle(e => e is DoorOrderReverted);
        var evt = (DoorOrderReverted)order.DomainEvents.First();
        evt.OrderId.Should().Be(order.Id);
        evt.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void RevertToDraft_WhenDraft_ReturnsInvalid()
    {
        var order = CreateDraftWithItem();

        var result = order.RevertToDraft();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void RevertToDraft_WhenInProduction_ReturnsInvalid()
    {
        // InProduction is not reachable via the current public FSM, so we construct
        // a Submitted order and verify the guard rejects a non-revertable status.
        var order = CreateSubmittedOrder();

        var result = order.RevertToDraft();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    // --- Version invariant across the full happy path ---

    [Fact]
    public void Version_IncrementsThroughFullCalculationPath()
    {
        var order = CreateDraftWithItem();
        order.Version.Should().Be(1);

        order.Submit();
        order.Version.Should().Be(2);

        order.MarkCalculating();
        order.Version.Should().Be(3);

        order.MarkCalculated();
        order.Version.Should().Be(4);

        order.RevertToDraft();
        order.Version.Should().Be(5);
    }
}
