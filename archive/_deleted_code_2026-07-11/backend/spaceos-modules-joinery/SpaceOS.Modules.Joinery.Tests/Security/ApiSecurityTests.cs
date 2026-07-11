using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.AddDoorItem;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.SubmitDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Security;

/// <summary>
/// Security gate tesztek — SEC-01..SEC-07 szabályok ellenőrzése.
/// </summary>
public class ApiSecurityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static DoorOrder MakeDraftOrderWithItem()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "A01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        return order;
    }

    // SEC-01: Cross-tenant isolation — GetByIdAsync TenantId-dal hívódik
    [Fact]
    public async Task SubmitHandler_PassesTenantId_ToRepository()
    {
        var repo = new Mock<IDoorOrderRepository>();
        var outbox = new Mock<IOutboxWriter>();
        var clock = new Mock<IClock>();
        var mediator = new Mock<IMediator>();
        var order = MakeDraftOrderWithItem();

        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        outbox.Setup(o => o.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        repo.Setup(r => r.UpdateAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cuttingProvider = new Mock<ICuttingProvider>();
        cuttingProvider
            .Setup(p => p.SubmitCuttingSheetAsync(It.IsAny<CuttingSheetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        var logger = new Mock<ILogger<SubmitDoorOrderCommandHandler>>();

        var sut = new SubmitDoorOrderCommandHandler(
            repo.Object, outbox.Object, clock.Object, mediator.Object,
            cuttingProvider.Object, logger.Object);
        await sut.Handle(new SubmitDoorOrderCommand(TenantId, order.Id), CancellationToken.None);

        // TenantId kötelezően átadódik → idegen tenant ORDER-je nem elérhető
        repo.Verify(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // BE-04: AddItem Submitted státuszban blokkolt
    [Fact]
    public async Task AddItem_ToSubmittedOrder_ReturnsError()
    {
        var repo = new Mock<IDoorOrderRepository>();
        var mediator = new Mock<IMediator>();
        var order = MakeDraftOrderWithItem();
        order.Submit(); // → Submitted

        repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var sut = new AddDoorItemCommandHandler(repo.Object, mediator.Object);
        var cmd = new AddDoorItemCommand(
            TenantId, order.Id, "B01", null, 1,
            "FAF_T", "Left",
            900m, 850m, 2100m, 2050m, 200m, 180m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status"); // BE-04
    }

    // SEC-07: MaxItems = 500
    [Fact]
    public void DoorOrder_MaxItems500_BlocksAddAt500()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;

        for (int i = 0; i < 500; i++)
            order.AddItem(DoorItem.Create(order.Id, $"I{i:D3}", 1, DoorType.FAF_T, OpeningDirection.Left, dims));

        var extra = DoorItem.Create(order.Id, "X001", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
        var result = order.AddItem(extra);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Items"); // SEC-07
    }

    // SEC-07: 499 item → még szabad
    [Fact]
    public void DoorOrder_At499Items_StillAcceptsOne()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;

        for (int i = 0; i < 499; i++)
            order.AddItem(DoorItem.Create(order.Id, $"I{i:D3}", 1, DoorType.FAF_T, OpeningDirection.Left, dims));

        var last = DoorItem.Create(order.Id, "X499", 1, DoorType.FAF_T, OpeningDirection.Left, dims);
        var result = order.AddItem(last);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(500);
    }

    // SEC-04: FlowEpicId kötelező (idegen tenant epic linkelés megakadályozása)
    [Fact]
    public void DoorOrder_Create_EmptyFlowEpicId_Blocked()
    {
        var result = DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "FlowEpicId");
    }
}
