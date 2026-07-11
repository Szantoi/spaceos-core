using FluentAssertions;
using MediatR;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.RevertDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class RevertDoorOrderHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly RevertDoorOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public RevertDoorOrderHandlerTests()
    {
        _repo.Setup(r => r.UpdateAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _sut = new RevertDoorOrderCommandHandler(_repo.Object, _mediator.Object);
    }

    private static DoorOrder MakeCalculatedOrder()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-REV", "Revert Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "R01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        order.Submit();
        order.MarkCalculating();
        order.MarkCalculated();
        return order;
    }

    private static DoorOrder MakeFailedOrder()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-FAIL", "Failed Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "F01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        order.Submit();
        order.MarkCalculating();
        order.MarkCalculationFailed("engine error");
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(orderId, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DoorOrder?)null);

        var result = await _sut.Handle(new RevertDoorOrderCommand(TenantId, orderId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_CalculatedOrder_RevertsToDraft()
    {
        var order = MakeCalculatedOrder();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        var result = await _sut.Handle(new RevertDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.Draft);
    }

    [Fact]
    public async Task Handle_CalculationFailedOrder_RevertsToDraft()
    {
        var order = MakeFailedOrder();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        var result = await _sut.Handle(new RevertDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(DoorOrderStatus.Draft);
    }

    [Fact]
    public async Task Handle_DraftOrder_ReturnsInvalid()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-D", null, Guid.NewGuid()).Value;
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        var result = await _sut.Handle(new RevertDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ValidRevert_CallsRepositoryUpdate()
    {
        var order = MakeCalculatedOrder();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        await _sut.Handle(new RevertDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        _repo.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }
}
