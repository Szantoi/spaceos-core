using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

public class DoorOrderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FlowEpicId = Guid.NewGuid();

    private static DoorOrder CreateValidOrder()
    {
        var result = DoorOrder.Create(TenantId, "PRJ-001", "Test Project", FlowEpicId);
        return result.Value;
    }

    private static DoorItem CreateValidItem(Guid orderId)
    {
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        return DoorItem.Create(orderId, "A01", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
    }

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Arrange & Act
        var result = DoorOrder.Create(TenantId, "PRJ-001", "Test Project", FlowEpicId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ProjectId.Should().Be("PRJ-001");
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.FlowEpicId.Should().Be(FlowEpicId);
    }

    [Fact]
    public void Create_WithEmptyFlowEpicId_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorOrder.Create(TenantId, "PRJ-001", "Test Project", Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "FlowEpicId");
    }

    [Fact]
    public void Create_WithEmptyProjectId_ReturnsInvalid()
    {
        // Arrange & Act
        var result = DoorOrder.Create(TenantId, "", "Test Project", FlowEpicId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "ProjectId");
    }

    [Fact]
    public void Create_RaisesOrderCreatedEvent()
    {
        // Arrange & Act
        var order = CreateValidOrder();

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is DoorOrderCreated);
        var evt = (DoorOrderCreated)order.DomainEvents.First();
        evt.TenantId.Should().Be(TenantId);
        evt.ProjectId.Should().Be("PRJ-001");
    }

    [Fact]
    public void AddItem_WhenDraft_Succeeds()
    {
        // Arrange
        var order = CreateValidOrder();
        var item = CreateValidItem(order.Id);

        // Act
        var result = order.AddItem(item);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_WhenSubmitted_ReturnsError() // BE-04
    {
        // Arrange
        var order = CreateValidOrder();
        var item = CreateValidItem(order.Id);
        order.AddItem(item);
        order.Submit();

        // Act
        var anotherItem = CreateValidItem(order.Id);
        var result = order.AddItem(anotherItem);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void AddItem_WhenAt500Items_ReturnsError() // SEC-07
    {
        // Arrange
        var order = CreateValidOrder();
        for (int i = 0; i < 500; i++)
        {
            var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
            var item = DoorItem.Create(order.Id, $"A{i:D3}", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
            order.AddItem(item);
        }

        // Act
        var extraItem = CreateValidItem(order.Id);
        var result = order.AddItem(extraItem);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Items");
    }

    [Fact]
    public void AddItem_RaisesItemAddedEvent()
    {
        // Arrange
        var order = CreateValidOrder();
        order.PopDomainEvents(); // clear Create event
        var item = CreateValidItem(order.Id);

        // Act
        order.AddItem(item);

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is DoorItemAdded);
        var evt = (DoorItemAdded)order.DomainEvents.First();
        evt.OrderId.Should().Be(order.Id);
        evt.ItemId.Should().Be(item.Id);
    }

    [Fact]
    public void Submit_WhenDraftWithItems_Succeeds()
    {
        // Arrange
        var order = CreateValidOrder();
        var item = CreateValidItem(order.Id);
        order.AddItem(item);

        // Act
        var result = order.Submit();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Submit_WhenDraftWithNoItems_ReturnsError()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Submit();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Items");
    }

    [Fact]
    public void Submit_WhenAlreadySubmitted_ReturnsError()
    {
        // Arrange
        var order = CreateValidOrder();
        var item = CreateValidItem(order.Id);
        order.AddItem(item);
        order.Submit();

        // Act
        var result = order.Submit();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void Submit_RaisesOrderSubmittedEvent()
    {
        // Arrange
        var order = CreateValidOrder();
        var item = CreateValidItem(order.Id);
        order.AddItem(item);
        order.PopDomainEvents(); // clear Create + ItemAdded events

        // Act
        order.Submit();

        // Assert
        order.DomainEvents.Should().ContainSingle(e => e is DoorOrderSubmitted);
        var evt = (DoorOrderSubmitted)order.DomainEvents.First();
        evt.OrderId.Should().Be(order.Id);
        evt.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void PopDomainEvents_ClearsEvents()
    {
        // Arrange
        var order = CreateValidOrder();
        order.DomainEvents.Should().NotBeEmpty();

        // Act
        var events = order.PopDomainEvents();

        // Assert
        events.Should().NotBeEmpty();
        order.DomainEvents.Should().BeEmpty();
    }
}
