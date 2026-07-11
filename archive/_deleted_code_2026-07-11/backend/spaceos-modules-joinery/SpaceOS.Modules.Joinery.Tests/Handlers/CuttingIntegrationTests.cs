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
/// Tests verifying ICuttingProvider integration in SubmitDoorOrderCommandHandler:
/// graceful degradation, correct DTO mapping, and call semantics.
/// </summary>
public class CuttingIntegrationTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IOutboxWriter> _outbox = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ICuttingProvider> _cuttingProvider = new();
    private readonly Mock<ILogger<SubmitDoorOrderCommandHandler>> _logger = new();
    private readonly SubmitDoorOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public CuttingIntegrationTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _outbox.Setup(o => o.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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

    private static DoorOrder MakeOrderWithItem(decimal doorWidth = 850m, decimal doorHeight = 2050m, decimal doorThickness = 180m)
    {
        var order = DoorOrder.Create(TenantId, "PRJ-CUT", "Cutting Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, doorWidth, 2100m, doorHeight, 200m, doorThickness).Value;
        var item = DoorItem.Create(order.Id, "A01", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
        order.AddItem(item);
        return order;
    }

    [Fact]
    public async Task Handle_ValidOrder_CallsCuttingProviderOnce()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        _cuttingProvider.Verify(
            p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidOrder_PassesCorrectOrderId()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SourceOrderId.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_ValidOrder_PassesCorrectLineCount()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        captured!.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ValidOrder_MapsItemDimensionsCorrectly()
    {
        var order = MakeOrderWithItem(doorWidth: 820m, doorHeight: 1980m, doorThickness: 44m);
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        var line = captured!.Lines[0];
        line.RawWidth.Should().Be(820m);
        line.RawHeight.Should().Be(1980m);
        line.Thickness.Should().Be(44m);
    }

    [Fact]
    public async Task Handle_ValidOrder_SetsCanRotateFalse()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        captured!.Lines[0].CanRotate.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CuttingProviderThrows_OrderSubmitStillSucceeds()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Cutting service unavailable"));

        var result = await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CuttingProviderThrows_WarningIsLogged()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("upstream failure"));

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CuttingProviderReturnsId_OrderSubmitSucceeds()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        var expectedSheetId = Guid.NewGuid();
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSheetId);

        var result = await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrderWithMultipleItems_CuttingSheetHasAllLines()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-CUT", "Multi", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "A01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        order.AddItem(DoorItem.Create(order.Id, "A02", 2, DoorType.FAF_T, OpeningDirection.Right, dims));
        order.AddItem(DoorItem.Create(order.Id, "A03", 1, DoorType.FAF_T, OpeningDirection.Left, dims));

        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        captured!.Lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ValidOrder_CuttingSheetHasTenantId()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        captured!.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task Handle_OrderNotFound_CuttingProviderNeverCalled()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(orderId, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DoorOrder?)null);

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, orderId), CancellationToken.None);

        _cuttingProvider.Verify(
            p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidOrder_CuttingSheetMaterialTypeIsDoor()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);

        CuttingSheetDto? captured = null;
        _cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CuttingSheetDto, CancellationToken>((sheet, _) => captured = sheet)
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        captured!.MaterialType.Should().Be("DOOR");
    }
}
