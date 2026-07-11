using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using SpaceOS.Modules.Abstractions.Tests.TestHelpers;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Manufacturing;

public class ProcessPlanTests
{
    private static readonly Guid TenantId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private readonly GraphCalculationEngine _engine = new();
    private readonly ManufacturingDerivationService _service = new();

    // --- Helpers ---

    /// Minimal linear chain: Root(virtual) → PanelA → PanelB
    private static ProductTemplate LinearChainTemplate()
    {
        var t = ProductTemplate.Create(TenantId, "door", "T").Value;
        var root   = t.AddSlot("Root",   "Root",  null,  null, 1, true,  null, 0).Value;
        var panelA = t.AddSlot("PanelA", "Panel", "MDF", 18m,  1, false, null, 1).Value;
        var panelB = t.AddSlot("PanelB", "Panel", "MDF", 18m,  1, false, null, 2).Value;

        t.AddConnection(root.Id, panelA.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(panelA.Id, panelB.Id, DimensionAxis.Width, RuleOperator.Subtract, 10m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);
        return t;
    }

    // --- Tests ---

    [Fact]
    public void RootSlot_HasDesignPhase_Order0()
    {
        var t      = LinearChainTemplate();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveProcessPlan(result);

        var rootStep = plan.First(s => s.SlotName == "Root");
        rootStep.Phase.Should().Be(ProcessPhase.Design);
        rootStep.Order.Should().Be(0);
    }

    [Fact]
    public void LinearChain_OrderedTopologically()
    {
        var t      = LinearChainTemplate();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveProcessPlan(result).OrderBy(s => s.Order).ToList();

        plan.Select(s => s.SlotName).Should().ContainInOrder("Root", "PanelA", "PanelB");
    }

    [Fact]
    public void ProcessPhase_Cutting_BeforeEdgeBanding()
    {
        // In FAF_T: Ajtólap is Cutting (order 2), Ajtó-Él-V/F are EdgeBanding (order 4-5)
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveProcessPlan(result);

        var cuttingOrder   = plan.First(s => s.Phase == ProcessPhase.Cutting).Order;
        var edgeBandOrders = plan.Where(s => s.Phase == ProcessPhase.EdgeBanding).Select(s => s.Order);

        edgeBandOrders.Should().AllSatisfy(o =>
            o.Should().BeGreaterThan(cuttingOrder, "EdgeBanding comes after Cutting"));
    }

    [Fact]
    public void AllSlots_IncludingVirtual_InProcessPlan()
    {
        // FAF_T has 6 slots (incl. Root virtual)
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveProcessPlan(result);

        plan.Should().HaveCount(6, "all 6 FAF_T slots must appear in the process plan");
        plan.Should().Contain(s => s.SlotName == "Root", "virtual Root slot is included");
    }

    [Fact]
    public void PhaseOrder_Reflects_TopologicalSort()
    {
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveProcessPlan(result).OrderBy(s => s.Order).ToList();

        // Orders must be unique and sequential
        var orders = plan.Select(s => s.Order).ToList();
        orders.Should().BeEquivalentTo(Enumerable.Range(0, plan.Count),
            "orders must be 0-based sequential without gaps");
    }

    [Fact]
    public void FafTTemplate_ProcessPlan_HasCuttingAndCncPhases()
    {
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveProcessPlan(result);

        plan.Should().Contain(s => s.Phase == ProcessPhase.Cutting,
            "BKM-panel → Ajtólap connection has Cutting phase");
        plan.Should().Contain(s => s.Phase == ProcessPhase.CNC,
            "Ajtólap → FrameCore-Alap connection has CNC phase");
        plan.Should().Contain(s => s.Phase == ProcessPhase.EdgeBanding,
            "Ajtólap → Ajtó-Él-V/F connections have EdgeBanding phase");
    }

    [Fact]
    public void MultipleIncomingPhases_PicksFirstNonDesignPhase()
    {
        // Slot with two incoming connections: one Design, one Cutting → picks Cutting
        var t    = ProductTemplate.Create(TenantId, "door", "T").Value;
        var root = t.AddSlot("Root",   "Root",  null,  null, 1, true,  null, 0).Value;
        var src  = t.AddSlot("Source", "Panel", "MDF", 18m,  1, false, null, 1).Value;
        var dst  = t.AddSlot("Target", "Panel", "MDF", 18m,  1, false, null, 2).Value;

        // src has one incoming Design connection from root
        t.AddConnection(root.Id, src.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);

        // dst gets two connections: one Design (W) and one Cutting (H)
        t.AddConnection(src.Id, dst.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(src.Id, dst.Id, DimensionAxis.Height, RuleOperator.Subtract, 4m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);

        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        var plan   = _service.DeriveProcessPlan(result);

        var targetStep = plan.First(s => s.SlotName == "Target");
        targetStep.Phase.Should().Be(ProcessPhase.Cutting,
            "when multiple incoming connections exist, the first non-Design phase is preferred");
    }
}
