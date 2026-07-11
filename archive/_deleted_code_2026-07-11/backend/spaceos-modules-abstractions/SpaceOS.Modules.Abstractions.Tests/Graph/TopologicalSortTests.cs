using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Graph;

public class TopologicalSortTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private readonly GraphCalculationEngine _engine = new();

    private static ProductTemplate MakeTemplate()
    {
        var t = ProductTemplate.Create(_tenantId, "door", "Test").Value;
        return t;
    }

    private static void AddSlot(ProductTemplate t, string name, string ct = "Root", bool isVirtual = false) =>
        t.AddSlot(name, ct, null, null, 1, isVirtual, null, 0);

    private static void Connect(ProductTemplate t, Guid parent, Guid child, DimensionAxis axis, RuleOperator op = RuleOperator.Identity, decimal operand = 0) =>
        t.AddConnection(parent, child, axis, op, operand, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

    [Fact]
    public void LinearChain_SortedCorrectly()
    {
        var t = MakeTemplate();
        var rA = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var rB = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var rC = t.AddSlot("C", "Panel", null, null, 1, false, null, 2).Value;
        Connect(t, rA.Id, rB.Id, DimensionAxis.Width);
        Connect(t, rB.Id, rC.Id, DimensionAxis.Width);

        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        result.Dimensions.ContainsKey(rA.Id).Should().BeTrue();
        result.Dimensions.ContainsKey(rB.Id).Should().BeTrue();
        result.Dimensions.ContainsKey(rC.Id).Should().BeTrue();
        // Width propagated Identity through chain
        result.Dimensions[rA.Id].Width.Should().Be(900);
        result.Dimensions[rB.Id].Width.Should().Be(900);
        result.Dimensions[rC.Id].Width.Should().Be(900);
    }

    [Fact]
    public void TwoChildrenFromRoot()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Frame", null, null, 1, false, null, 2).Value;
        Connect(t, root.Id, b.Id, DimensionAxis.Width);
        Connect(t, root.Id, c.Id, DimensionAxis.Height);

        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        result.Dimensions[root.Id].Width.Should().Be(900);
        result.Dimensions[b.Id].Width.Should().Be(900);
        result.Dimensions[c.Id].Height.Should().Be(2100);
    }

    [Fact]
    public void Diamond_SortedCorrectly()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Frame", null, null, 1, false, null, 2).Value;
        var leaf = t.AddSlot("Leaf", "Panel", null, null, 1, false, null, 3).Value;
        Connect(t, root.Id, b.Id, DimensionAxis.Width);
        Connect(t, root.Id, c.Id, DimensionAxis.Height);
        Connect(t, b.Id, leaf.Id, DimensionAxis.Width);

        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        result.Dimensions.Keys.Should().Contain(new[] { root.Id, b.Id, c.Id, leaf.Id });
    }

    [Fact]
    public void EmptyTemplate_ReturnsEmpty()
    {
        var t = MakeTemplate();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        result.Dimensions.Should().BeEmpty();
        result.CuttingList.Should().BeEmpty();
    }

    [Fact]
    public void SingleSlot_ReturnsSingleItem()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var result = _engine.Calculate(t, new DimensionInput(500, 1000, 20));
        result.Dimensions.Should().ContainKey(root.Id);
        result.Dimensions[root.Id].Width.Should().Be(500);
    }

    [Fact]
    public void SortIsDeterministic_SameInputSameOutput()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        Connect(t, root.Id, b.Id, DimensionAxis.Width);

        var r1 = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        var r2 = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        r1.Dimensions[root.Id].Should().Be(r2.Dimensions[root.Id]);
        r1.Dimensions[b.Id].Should().Be(r2.Dimensions[b.Id]);
    }

    [Fact]
    public void ThreeLevel_DeepChain_NoStackOverflow()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var prev = root;
        for (int i = 0; i < 49; i++)
        {
            var slot = t.AddSlot($"Slot{i}", "Panel", null, null, 1, false, null, i + 1).Value;
            Connect(t, prev.Id, slot.Id, DimensionAxis.Width);
            prev = slot;
        }
        // 50 total slots
        var act = () => _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        act.Should().NotThrow();
    }

    [Fact]
    public void MaxDepth_200Slots_CompletesUnder100ms()
    {
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var prev = root;
        for (int i = 0; i < 199; i++)
        {
            var slot = t.AddSlot($"S{i}", "Panel", null, null, 1, false, null, i + 1).Value;
            Connect(t, prev.Id, slot.Id, DimensionAxis.Width);
            prev = slot;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }
}
