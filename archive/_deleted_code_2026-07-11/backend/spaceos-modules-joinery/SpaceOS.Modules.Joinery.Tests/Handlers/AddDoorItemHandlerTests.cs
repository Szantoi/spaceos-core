using FluentAssertions;
using MediatR;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.AddDoorItem;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class AddDoorItemHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly AddDoorItemCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public AddDoorItemHandlerTests()
    {
        _repo.Setup(r => r.UpdateAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _sut = new AddDoorItemCommandHandler(_repo.Object, _mediator.Object);
    }

    private static DoorOrder MakeDraftOrder()
        => DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.NewGuid()).Value;

    private static AddDoorItemCommand ValidCommand(Guid orderId) => new(
        TenantId, orderId, "A01", "Bejárati ajtó", 1,
        "FAF_T", "Left",
        900m, 850m, 2100m, 2050m, 200m, 180m);

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(orderId, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DoorOrder?)null);

        var result = await _sut.Handle(ValidCommand(orderId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithGuid()
    {
        var order = MakeDraftOrder();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        var result = await _sut.Handle(ValidCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_InvalidDimensions_ReturnsInvalid()
    {
        var order = MakeDraftOrder();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        // DoorWidth > WallOpeningWidth → invalid
        var cmd = new AddDoorItemCommand(
            TenantId, order.Id, "A01", null, 1,
            "FAF_T", "Left",
            800m, 850m, 2100m, 2050m, 200m, 180m);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "DoorWidth");
    }
}
