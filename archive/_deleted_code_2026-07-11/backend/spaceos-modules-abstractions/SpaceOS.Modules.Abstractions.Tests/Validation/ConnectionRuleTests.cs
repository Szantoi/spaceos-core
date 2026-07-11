using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Validation;

public class ConnectionRuleTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static ProductTemplate MakeTemplate() =>
        ProductTemplate.Create(_tenantId, "door", "Test").Value;

    [Fact]
    public void SelfLoop_AddConnection_ReturnsError()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;

        var result = t.AddConnection(a.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Self-loop"));
    }

    [Fact]
    public void MaxConnections_501st_ReturnsError()
    {
        var t = MakeTemplate();
        // 1 root + 199 children = 200 slots (max)
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var children = new List<Guid>();
        for (int i = 0; i < 199; i++)
        {
            var s = t.AddSlot($"S{i}", "Panel", null, null, 1, false, null, i + 1).Value;
            children.Add(s.Id);
        }

        // Fill 500 connections: 199 Width + 199 Height + 102 Depth = 500
        foreach (var cid in children)
            t.AddConnection(root.Id, cid, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
                JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        foreach (var cid in children)
            t.AddConnection(root.Id, cid, DimensionAxis.Height, RuleOperator.Identity, 0, null, null,
                JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        for (int i = 0; i < 102; i++)
            t.AddConnection(root.Id, children[i], DimensionAxis.Depth, RuleOperator.Identity, 0, null, null,
                JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        // 501st should fail
        var result = t.AddConnection(root.Id, children[102], DimensionAxis.Depth, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("500"));
    }

    [Fact]
    public void MaxSlots_201st_AddSlot_ReturnsError()
    {
        var t = MakeTemplate();
        for (int i = 0; i < 200; i++)
            t.AddSlot($"S{i}", "Panel", null, null, 1, false, null, i);

        var result = t.AddSlot("TooMany", "Panel", null, null, 1, false, null, 200);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("200"));
    }

    [Fact]
    public void InvalidComponentType_Create_ReturnsInvalid()
    {
        var t = MakeTemplate();
        var result = t.AddSlot("A", "InvalidType", null, null, 1, false, null, 0);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void InvalidOperand_Negative_IsAllowed_ForConstant()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 1).Value;

        // Constant with negative operand should be allowed (e.g. offset = -5)
        var result = t.AddConnection(root.Id, child.Id, DimensionAxis.Depth, RuleOperator.Constant, -5m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        result.IsSuccess.Should().BeTrue();
    }
}
