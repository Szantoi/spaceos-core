using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Graph;

public class CycleDetectionTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private readonly GraphCalculationEngine _engine = new();

    private static ProductTemplate MakeTemplate() =>
        ProductTemplate.Create(_tenantId, "door", "Test").Value;

    private static void Connect(ProductTemplate t, Guid parent, Guid child) =>
        t.AddConnection(parent, child, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

    [Fact]
    public void DirectCycle_AToB_BToA_RejectsAtWriteTime()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        Connect(t, a.Id, b.Id); // A→B OK

        // B→A would create cycle A→B→A — must be rejected at write-time
        var result = t.AddConnection(b.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cycle detected"));
    }

    [Fact]
    public void IndirectCycle_A_B_C_A_RejectsAtWriteTime()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Panel", null, null, 1, false, null, 2).Value;
        Connect(t, a.Id, b.Id); // A→B OK
        Connect(t, b.Id, c.Id); // B→C OK

        // C→A would close cycle A→B→C→A — must be rejected at write-time
        var result = t.AddConnection(c.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cycle detected"));
    }

    [Fact]
    public void NoCycle_ValidDAG_DoesNotThrow()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Panel", null, null, 1, false, null, 2).Value;
        Connect(t, a.Id, b.Id);
        Connect(t, b.Id, c.Id);

        var act = () => _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        act.Should().NotThrow();
    }

    [Fact]
    public void SelfLoop_AddConnection_ReturnsForbidden()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;

        var result = t.AddConnection(a.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Self-loop"));
    }

    [Fact]
    public void DagWithDiamond_NoCycle_DoesNotThrow()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Frame", null, null, 1, false, null, 2).Value;
        var leaf = t.AddSlot("Leaf", "Panel", null, null, 1, false, null, 3).Value;
        Connect(t, root.Id, b.Id);
        Connect(t, root.Id, c.Id);
        Connect(t, b.Id, leaf.Id);
        Connect(t, c.Id, leaf.Id);

        var act = () => _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        act.Should().NotThrow();
    }
}
