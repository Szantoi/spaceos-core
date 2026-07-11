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

/// <summary>
/// Tests for the outbox entry creation behaviour introduced in Track B.
/// </summary>
public class SubmitDoorOrderOutboxTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IOutboxWriter> _outbox = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ICuttingProvider> _cuttingProvider = new();
    private readonly Mock<ILogger<SubmitDoorOrderCommandHandler>> _logger = new();
    private readonly SubmitDoorOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset FrozenTime = new(2026, 4, 10, 12, 0, 0, TimeSpan.Zero);

    public SubmitDoorOrderOutboxTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FrozenTime);
        _outbox.Setup(o => o.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _sut = new SubmitDoorOrderCommandHandler(
            _repo.Object, _outbox.Object, _clock.Object, _mediator.Object,
            _cuttingProvider.Object, _logger.Object);
    }

    private static DoorOrder MakeOrderWithItems(int itemCount)
    {
        var order = DoorOrder.Create(TenantId, "PRJ-OB", "Outbox Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        for (var i = 0; i < itemCount; i++)
            order.AddItem(DoorItem.Create(order.Id, $"A{i:D2}", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        return order;
    }

    [Fact]
    public async Task Handle_Submit_AddsOneOutboxEntryPerItem()
    {
        var order = MakeOrderWithItems(3);
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>? capturedEntries = null;
        _outbox.Setup(o => o.AddRange(It.IsAny<IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>>()))
               .Callback<IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>>(e => capturedEntries = e);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        capturedEntries.Should().NotBeNull();
        capturedEntries!.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Submit_OutboxEntriesHaveCorrectEventType()
    {
        var order = MakeOrderWithItems(1);
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>? capturedEntries = null;
        _outbox.Setup(o => o.AddRange(It.IsAny<IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>>()))
               .Callback<IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>>(e => capturedEntries = e);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        capturedEntries!.Should().AllSatisfy(e => e.EventType.Should().Be("DoorItemCalculationRequested"));
    }

    [Fact]
    public async Task Handle_Submit_OutboxEntriesHaveCorrectTenantId()
    {
        var order = MakeOrderWithItems(2);
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>? capturedEntries = null;
        _outbox.Setup(o => o.AddRange(It.IsAny<IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>>()))
               .Callback<IEnumerable<SpaceOS.Modules.Joinery.Domain.Entities.JoineryOutboxEntry>>(e => capturedEntries = e);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        capturedEntries!.Should().AllSatisfy(e => e.TenantId.Should().Be(TenantId));
    }

    [Fact]
    public async Task Handle_Submit_CallsOutboxSaveAsync()
    {
        var order = MakeOrderWithItems(1);
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        _outbox.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Submit_TransitionsOrderToSubmitted()
    {
        var order = MakeOrderWithItems(1);
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        order.Status.Should().Be(DoorOrderStatus.Submitted);
    }
}
