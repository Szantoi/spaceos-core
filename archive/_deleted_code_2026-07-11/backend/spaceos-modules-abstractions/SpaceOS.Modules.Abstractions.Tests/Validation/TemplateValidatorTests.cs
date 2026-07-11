using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Validation;

public class TemplateValidatorTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private readonly TemplateValidatorService _validator = new();

    private static ProductTemplate MakeTemplate() =>
        ProductTemplate.Create(_tenantId, "door", "Test").Value;

    private static void Connect(ProductTemplate t, Guid parent, Guid child) =>
        t.AddConnection(parent, child, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

    [Fact]
    public void ValidTemplate_ConnectedDAG_ReturnsSuccess()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 1).Value;
        Connect(t, root.Id, child.Id);

        var result = _validator.Validate(t);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CycleEdge_IsRejectedAtWriteTime_GraphRemainsValid()
    {
        // With write-time cycle detection, B→A is rejected before it enters _connections.
        // The resulting graph (A→B only) is valid, so the validator returns success.
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        Connect(t, a.Id, b.Id);
        var cycleResult = t.AddConnection(b.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        cycleResult.IsSuccess.Should().BeFalse("cycle must be rejected at write-time");
        // The graph now only has A→B — a valid DAG with a single root
        var validationResult = _validator.Validate(t);
        validationResult.IsSuccess.Should().BeTrue("remaining graph is a valid DAG");
    }

    [Fact]
    public void MultipleRoots_ReturnsInvalid()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Root", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Frame", null, null, 1, false, null, 2).Value;
        // Both A and B have no parents → 2 roots
        Connect(t, a.Id, c.Id);
        // B has no connection out or in but as we also add an edge from B:
        // Actually just having 2 nodes with no incoming edges = 2 roots
        // A→C, B has no incoming edge and no outgoing edge = orphan actually
        // Let's make B also connect to C
        t.AddConnection(b.Id, c.Id, DimensionAxis.Height, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        var result = _validator.Validate(t);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("Multiple root"));
    }

    [Fact]
    public void OrphanSlot_ReturnsInvalid()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 1).Value;
        var orphan = t.AddSlot("Orphan", "Panel", null, null, 1, false, null, 2).Value;
        Connect(t, root.Id, child.Id);
        // Orphan: no edges at all

        var result = _validator.Validate(t);
        // Orphan with no incoming edges appears as a second "root" → validation fails
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void DisconnectedGraph_ReturnsInvalid()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 1).Value;
        var isolated1 = t.AddSlot("I1", "Panel", null, null, 1, false, null, 2).Value;
        var isolated2 = t.AddSlot("I2", "Panel", null, null, 1, false, null, 3).Value;
        Connect(t, root.Id, child.Id);
        Connect(t, isolated1.Id, isolated2.Id); // disconnected subgraph

        var result = _validator.Validate(t);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("Disconnected") || e.ErrorMessage.Contains("root"));
    }
}
