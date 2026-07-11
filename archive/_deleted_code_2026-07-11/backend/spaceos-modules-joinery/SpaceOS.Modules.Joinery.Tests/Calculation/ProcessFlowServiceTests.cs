using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Infrastructure.Services;

namespace SpaceOS.Modules.Joinery.Tests.Calculation;

public class ProcessFlowServiceTests
{
    private readonly ProcessFlowService _sut = new();

    private static ProcessTaskTemplate MakeTemplate(
        string taskId = "T01",
        string shortName = "Keret vágás",
        int unitTimeSec = 300,
        int headcount = 1,
        string? parentTaskId = null) => new()
    {
        TaskId = taskId,
        ShortName = shortName,
        Description = "Leírás",
        Department = "Gyártás",
        UnitTimeSec = unitTimeSec,
        Headcount = headcount,
        ParentTaskId = parentTaskId
    };

    private static DoorOrder MakeOrder()
        => DoorOrder.Create(Guid.NewGuid(), "PRJ-001", "Test", Guid.NewGuid()).Value;

    [Fact]
    public void GenerateProcessPlan_EmptyTemplates_ReturnsEmpty()
    {
        var order = MakeOrder();

        var result = _sut.GenerateProcessPlan(order, []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateProcessPlan_MapsAllFields()
    {
        var order = MakeOrder();
        var template = MakeTemplate("T01", "Keret vágás", 120, 2, null);

        var result = _sut.GenerateProcessPlan(order, [template]);

        result.Should().HaveCount(1);
        result[0].TaskId.Should().Be("T01");
        result[0].ShortName.Should().Be("Keret vágás");
        result[0].Headcount.Should().Be(2);
    }

    [Fact]
    public void GenerateProcessPlan_UnitTimeSec_ConvertsToTimeSpan()
    {
        var order = MakeOrder();
        var template = MakeTemplate(unitTimeSec: 180);

        var result = _sut.GenerateProcessPlan(order, [template]);

        result[0].UnitTime.Should().Be(TimeSpan.FromSeconds(180));
    }

    [Fact]
    public void GenerateProcessPlan_PreservesParentTaskId()
    {
        var order = MakeOrder();
        var parent = MakeTemplate("T01", "Szülő", parentTaskId: null);
        var child = MakeTemplate("T02", "Gyerek", parentTaskId: "T01");

        var result = _sut.GenerateProcessPlan(order, [parent, child]);

        result.Should().HaveCount(2);
        result[1].ParentTaskId.Should().Be("T01");
        result[0].ParentTaskId.Should().BeNull();
    }
}
