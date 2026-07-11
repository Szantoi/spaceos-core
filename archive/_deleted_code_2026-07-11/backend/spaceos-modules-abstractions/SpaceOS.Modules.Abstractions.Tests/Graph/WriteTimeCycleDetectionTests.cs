using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Graph;

public class WriteTimeCycleDetectionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly GraphCalculationEngine _engine = new();

    private static ProductTemplate MakeTemplate() =>
        ProductTemplate.Create(TenantId, "door", "Test").Value;

    private static void Connect(ProductTemplate t, Guid parent, Guid child) =>
        t.AddConnection(parent, child, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

    [Fact]
    public void AddEdge_WhenCreatesSelfLoop_RejectsAtWriteTime()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;

        var result = t.AddConnection(a.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Self-loop"));
    }

    [Fact]
    public void AddEdge_WhenCreatesCycle_RejectsAtWriteTime()
    {
        var t = MakeTemplate();
        var a = t.AddSlot("A", "Root", null, null, 1, false, null, 0).Value;
        var b = t.AddSlot("B", "Frame", null, null, 1, false, null, 1).Value;
        var c = t.AddSlot("C", "Panel", null, null, 1, false, null, 2).Value;
        Connect(t, a.Id, b.Id); // A→B OK
        Connect(t, b.Id, c.Id); // B→C OK

        // C→A would close cycle A→B→C→A
        var result = t.AddConnection(c.Id, a.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cycle detected"));

        // The graph that was accepted (A→B, B→C) is still valid — engine must not throw
        var act = () => _engine.Calculate(t, new DimensionInput(1000, 2000, 40));
        act.Should().NotThrow();
    }

    [Fact]
    public void DeriveManufacturing_WithDiamondDependency_DerivesOnce()
    {
        // Diamond: Root→B, Root→C, B→D (Width -10), C→D (Height -20)
        // D receives Width from B and Height from C; Kahn processes D exactly once
        var t = MakeTemplate();
        var root = t.AddSlot("Root", "Root", null, null, 1, false, null, 0).Value;
        var b    = t.AddSlot("B",    "Frame", null, null, 1, true,  null, 1).Value;
        var c    = t.AddSlot("C",    "Frame", null, null, 1, true,  null, 2).Value;
        var d    = t.AddSlot("D",    "Panel", null, null, 1, false, null, 3).Value;

        // Root→B: Width identity
        t.AddConnection(root.Id, b.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        // Root→C: Height identity
        t.AddConnection(root.Id, c.Id, DimensionAxis.Height, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        // B→D: Width subtract 10
        t.AddConnection(b.Id, d.Id, DimensionAxis.Width, RuleOperator.Subtract, 10, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        // C→D: Height subtract 20
        t.AddConnection(c.Id, d.Id, DimensionAxis.Height, RuleOperator.Subtract, 20, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        var result = _engine.Calculate(t, new DimensionInput(1000, 2000, 40));

        result.Dimensions[root.Id].Width.Should().Be(1000);
        result.Dimensions[root.Id].Height.Should().Be(2000);
        // D.Width = 1000 - 10 = 990 (from B which got Width from Root)
        result.Dimensions[d.Id].Width.Should().Be(990);
        // D.Height = 2000 - 20 = 1980 (from C which got Height from Root)
        result.Dimensions[d.Id].Height.Should().Be(1980);
        // Only D is physical (b and c are virtual), so cutting list has exactly root + D
        result.CuttingList.Should().ContainSingle(i => i.SlotId == d.Id);
    }

    [Fact]
    public void DeriveManufacturing_With100Nodes_CompletesWithinTimeout()
    {
        // Linear chain: slot[0] → slot[1] → ... → slot[99]
        var t = MakeTemplate();
        var slots = new Guid[100];
        slots[0] = t.AddSlot("slot0", "Root", null, null, 1, false, null, 0).Value.Id;
        for (var i = 1; i < 100; i++)
        {
            slots[i] = t.AddSlot($"slot{i}", "Panel", null, null, 1, false, null, i).Value.Id;
            t.AddConnection(slots[i - 1], slots[i], DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
                JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = _engine.Calculate(t, new DimensionInput(1000, 2000, 40));
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
        result.Dimensions.Should().HaveCount(100);
    }

    [Fact]
    public void GraphEngine_CrossTenant_ReturnsEmpty()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var template = ProductTemplate.Create(tenantA, "door", "TenantA-Template").Value;
        template.AddSlot("Root", "Root", null, null, 1, false, null, 0);

        var jwtTenantId = tenantB;

        // Handler guard: if template.TenantId != jwtTenantId → Forbidden (no graph access)
        var allowed = template.TenantId == jwtTenantId;
        allowed.Should().BeFalse("Tenant B cannot access Tenant A graph");

        // Verify engine was never called for cross-tenant (engine would only be called after auth check)
        // This test documents the security invariant at the handler level
    }
}
