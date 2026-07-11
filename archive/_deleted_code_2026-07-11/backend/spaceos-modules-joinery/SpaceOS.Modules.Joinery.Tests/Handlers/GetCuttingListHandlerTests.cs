using FluentAssertions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class GetCuttingListHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IDoorRulesRepository> _rules = new();
    private readonly Mock<IDoorCalculationService> _calc = new();
    private readonly GetCuttingListQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetCuttingListHandlerTests()
    {
        _sut = new GetCuttingListQueryHandler(_repo.Object, _rules.Object, _calc.Object);
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

        var result = await _sut.Handle(new GetCuttingListQuery(TenantId, orderId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_MissingDoorTypeRule_ReturnsError()
    {
        var order = MakeOrderWithItem();
        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        _rules.Setup(r => r.GetDoorTypeRuleAsync("FAF_T", It.IsAny<CancellationToken>()))
              .ReturnsAsync((DoorTypeRule?)null);
        _rules.Setup(r => r.GetPartDimensionRulesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);
        _rules.Setup(r => r.GetGlobalConstantsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var result = await _sut.Handle(new GetCuttingListQuery(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("FAF_T"));
    }

    [Fact]
    public async Task Handle_ValidOrderWithItems_ReturnsCuttingList()
    {
        var order = MakeOrderWithItem();
        var rule = new DoorTypeRule { DoorType = "FAF_T", BkmWidthFixed = 8m, BkmHeightFixed = 4m };
        var constant = new GlobalConstant { Key = "CuttingOversize", Value = 1m };
        var cuttingItem = new CuttingListItem("A01", "Keret", "MDF", 18m, 859m, 2055m, 1, "Frame");

        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        _rules.Setup(r => r.GetDoorTypeRuleAsync("FAF_T", It.IsAny<CancellationToken>()))
              .ReturnsAsync(rule);
        _rules.Setup(r => r.GetPartDimensionRulesAsync("FAF_T", It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);
        _rules.Setup(r => r.GetGlobalConstantsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([constant]);
        _calc.Setup(c => c.CalculateCuttingList(It.IsAny<DoorItem>(), rule,
                It.IsAny<IReadOnlyList<PartDimensionRule>>(), constant))
             .Returns([cuttingItem]);

        var result = await _sut.Handle(new GetCuttingListQuery(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(order.Id);
        result.Value.Items.Should().ContainSingle();
    }
}
