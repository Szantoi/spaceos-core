using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.SubmitDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class SubmitDoorOrderHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IOutboxWriter> _outbox = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ICuttingProvider> _cuttingProvider = new();
    private readonly Mock<ILogger<SubmitDoorOrderCommandHandler>> _logger = new();
    private readonly SubmitDoorOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public SubmitDoorOrderHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _outbox.Setup(o => o.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _sut = new SubmitDoorOrderCommandHandler(
            _repo.Object,
            _outbox.Object,
            _clock.Object,
            _mediator.Object,
            _cuttingProvider.Object,
            _logger.Object);
    }

    private static DoorOrder MakeOrderWithItem()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        var item = DoorItem.Create(order.Id, "A01", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
        order.AddItem(item);
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(orderId, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DoorOrder?)null);

        var result = await _sut.Handle(new SubmitDoorOrderCommand(TenantId, orderId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidOrder_ReturnsSuccess()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        var result = await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidOrder_PersistsViaOutboxWriter()
    {
        // Submit now saves order state and outbox entries atomically via IOutboxWriter.SaveAsync,
        // not via IDoorOrderRepository.UpdateAsync (both share the same DbContext in production).
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        _outbox.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
