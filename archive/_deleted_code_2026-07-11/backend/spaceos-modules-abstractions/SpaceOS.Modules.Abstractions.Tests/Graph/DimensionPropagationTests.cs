using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Graph;

public class DimensionPropagationTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private readonly GraphCalculationEngine _engine = new();

    private (ProductTemplate template, Guid rootId, Guid childId) MakePair(
        RuleOperator op, DimensionAxis axis = DimensionAxis.Width, decimal operand = 0,
        int? multiplierCount = null, Guid? secondaryParent = null)
    {
        var t = ProductTemplate.Create(_tenantId, "door", "Test").Value;
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 1).Value;
        t.AddConnection(root.Id, child.Id, axis, op, operand, multiplierCount, secondaryParent,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        return (t, root.Id, child.Id);
    }

    [Fact]
    public void Identity_ChildEqualsParent()
    {
        var (t, _, childId) = MakePair(RuleOperator.Identity, DimensionAxis.Width);
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        r.Dimensions[childId].Width.Should().Be(900);
    }

    [Fact]
    public void Subtract_ChildIsParentMinusOperand()
    {
        var (t, _, childId) = MakePair(RuleOperator.Subtract, DimensionAxis.Width, 10);
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        r.Dimensions[childId].Width.Should().Be(890);
    }

    [Fact]
    public void Add_ChildIsParentPlusOperand()
    {
        var (t, _, childId) = MakePair(RuleOperator.Add, DimensionAxis.Width, 5);
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        r.Dimensions[childId].Width.Should().Be(905);
    }

    [Fact]
    public void SubtractN_ChildIsParentMinusNTimesOperand()
    {
        var (t, _, childId) = MakePair(RuleOperator.SubtractN, DimensionAxis.Width, 10, 2);
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        r.Dimensions[childId].Width.Should().Be(880); // 900 - (10 * 2)
    }

    [Fact]
    public void Max_ChildIsMaxOfTwoParentsMinusOperand()
    {
        var t = ProductTemplate.Create(_tenantId, "door", "Test").Value;
        var root1 = t.AddSlot("R1", "Root", null, null, 1, false, null, 0).Value;
        var root2 = t.AddSlot("R2", "Root", null, null, 1, false, null, 1).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 2).Value;

        // R1→Child with Max, secondary=R2
        t.AddConnection(root1.Id, child.Id, DimensionAxis.Width, RuleOperator.Max, 5, null, root2.Id,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        // Manually set R2 width via identity conn from R1
        t.AddConnection(root1.Id, root2.Id, DimensionAxis.Width, RuleOperator.Subtract, 200, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        // Max(900, 700) - 5 = 900 - 5 = 895
        r.Dimensions[child.Id].Width.Should().Be(895);
    }

    [Fact]
    public void Min_ChildIsMinOfTwoParentsMinusOperand()
    {
        var t = ProductTemplate.Create(_tenantId, "door", "Test").Value;
        var root1 = t.AddSlot("R1", "Root", null, null, 1, false, null, 0).Value;
        var root2 = t.AddSlot("R2", "Root", null, null, 1, false, null, 1).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 2).Value;

        t.AddConnection(root1.Id, root2.Id, DimensionAxis.Width, RuleOperator.Subtract, 200, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        t.AddConnection(root1.Id, child.Id, DimensionAxis.Width, RuleOperator.Min, 5, null, root2.Id,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        // Min(900, 700) - 5 = 700 - 5 = 695
        r.Dimensions[child.Id].Width.Should().Be(695);
    }

    [Fact]
    public void Constant_ChildIsOperand_RegardlessOfParent()
    {
        var (t, _, childId) = MakePair(RuleOperator.Constant, DimensionAxis.Width, 300);
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        r.Dimensions[childId].Width.Should().Be(300);
    }

    [Fact]
    public void UnknownOperator_ThrowsDomainException()
    {
        var t = ProductTemplate.Create(_tenantId, "door", "Test").Value;
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var child = t.AddSlot("Child", "Frame", null, null, 1, false, null, 1).Value;

        // Force an invalid operator through direct construction using reflection is complex.
        // Instead test that the engine throws when an unmapped enum cast occurs.
        // We simulate by calling with an int cast to RuleOperator outside enum range
        var badOp = (RuleOperator)99;
        var act = () => t.AddConnection(root.Id, child.Id, DimensionAxis.Width, badOp, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        act.Should().Throw<DomainException>().WithMessage("*Unknown*");
    }
}
