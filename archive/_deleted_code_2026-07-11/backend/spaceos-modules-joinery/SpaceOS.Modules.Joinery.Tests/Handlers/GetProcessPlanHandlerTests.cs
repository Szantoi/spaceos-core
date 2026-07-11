using FluentAssertions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProcessPlan;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class GetProcessPlanHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IDoorRulesRepository> _rules = new();
    private readonly Mock<IProcessFlowService> _service = new();
    private readonly GetProcessPlanQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetProcessPlanHandlerTests()
    {
        _sut = new GetProcessPlanQueryHandler(_repo.Object, _rules.Object, _service.Object);
    }

    private static DoorOrder MakeOrder()
        => DoorOrder.Create(TenantId, "PRJ-001", "Test", Guid.NewGuid()).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(orderId, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DoorOrder?)null);

        var result = await _sut.Handle(new GetProcessPlanQuery(TenantId, orderId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidOrder_ReturnsPlanWithTasks()
    {
        var order = MakeOrder();
        var templates = new List<ProcessTaskTemplate>
        {
            new() { TaskId = "T01", ShortName = "Vágás", UnitTimeSec = 60, Headcount = 1 }
        };
        var tasks = new List<ProcessTask>
        {
            new("T01", "Vágás", null, null, TimeSpan.FromSeconds(60), 1, null)
        };

        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        _rules.Setup(r => r.GetProcessTaskTemplatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(templates.AsReadOnly());
        _service.Setup(s => s.GenerateProcessPlan(order, It.IsAny<IReadOnlyList<ProcessTaskTemplate>>()))
                .Returns(tasks.AsReadOnly());

        var result = await _sut.Handle(new GetProcessPlanQuery(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(order.Id);
        result.Value.Tasks.Should().HaveCount(1);
        result.Value.Tasks[0].TaskId.Should().Be("T01");
    }

    [Fact]
    public async Task Handle_ValidOrder_EmptyTemplates_ReturnEmptyPlan()
    {
        var order = MakeOrder();

        _repo.Setup(r => r.GetByIdAsync(order.Id, TenantId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(order);
        _rules.Setup(r => r.GetProcessTaskTemplatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProcessTaskTemplate>().AsReadOnly());
        _service.Setup(s => s.GenerateProcessPlan(order, It.IsAny<IReadOnlyList<ProcessTaskTemplate>>()))
                .Returns(new List<ProcessTask>().AsReadOnly());

        var result = await _sut.Handle(new GetProcessPlanQuery(TenantId, order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tasks.Should().BeEmpty();
    }
}
