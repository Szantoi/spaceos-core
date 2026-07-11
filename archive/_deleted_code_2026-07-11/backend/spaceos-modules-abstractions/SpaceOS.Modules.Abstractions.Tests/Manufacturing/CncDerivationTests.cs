using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using SpaceOS.Modules.Abstractions.Tests.TestHelpers;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Manufacturing;

public class CncDerivationTests
{
    private static readonly Guid TenantId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private readonly GraphCalculationEngine _engine = new();
    private readonly ManufacturingDerivationService _service = new();

    // --- Helpers ---

    private static ProductTemplate SingleGrooveTemplate()
    {
        var t = ProductTemplate.Create(TenantId, "door", "T").Value;
        var root  = t.AddSlot("Root",  "Root",  null,  null, 1, true,  null, 0).Value;
        var panel = t.AddSlot("Panel", "Panel", "MDF", 18m,  1, false, null, 1).Value;
        t.AddConnection(root.Id, panel.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC,
            grooveDepth: 6m, grooveWidth: 10m);
        return t;
    }

    private static ProductTemplate NoneOperationTemplate()
    {
        var t = ProductTemplate.Create(TenantId, "door", "T").Value;
        var root  = t.AddSlot("Root",  "Root",  null,  null, 1, true,  null, 0).Value;
        var panel = t.AddSlot("Panel", "Panel", "MDF", 18m,  1, false, null, 1).Value;
        t.AddConnection(root.Id, panel.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, panel.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        return t;
    }

    private static ProductTemplate DrillTemplate()
    {
        var t = ProductTemplate.Create(TenantId, "door", "T").Value;
        var root  = t.AddSlot("Root",   "Root",  null,  null, 1, true,  null, 0).Value;
        var panel = t.AddSlot("Dowel",  "Panel", "MDF", 18m,  1, false, null, 1).Value;
        t.AddConnection(root.Id, panel.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Dowel, MachiningOperation.Drill, ProcessPhase.CNC,
            drillDiameter: 8m, drillDepth: 35m);
        return t;
    }

    // --- Tests ---

    [Fact]
    public void SingleGrooveConnection_ProducesCncOperation()
    {
        var t      = SingleGrooveTemplate();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveCncPlan(result);

        plan.Should().HaveCount(1);
        plan[0].Operation.Should().Be(MachiningOperation.Groove);
        plan[0].SlotName.Should().Be("Panel");
    }

    [Fact]
    public void NoneOperation_SkippedInCncPlan()
    {
        var t      = NoneOperationTemplate();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveCncPlan(result);

        plan.Should().BeEmpty();
    }

    [Fact]
    public void MultipleConnectionsPerSlot_AllOperationsDerived()
    {
        // FAF_T: FrameCore-Alap has 2 incoming Groove connections (W and H)
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveCncPlan(result);

        var frameCoreOps = plan.Where(o => o.SlotName == "FrameCore-Alap").ToList();
        frameCoreOps.Should().HaveCount(2, "two Groove connections on Width and Height axes");
        frameCoreOps.Should().AllSatisfy(op => op.Operation.Should().Be(MachiningOperation.Groove));
    }

    [Fact]
    public void VirtualSlot_ExcludedFromCncPlan()
    {
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveCncPlan(result);

        plan.Should().NotContain(op => op.SlotName == "Root",
            "Root slot is virtual and must be excluded");
    }

    [Fact]
    public void SlotNameSanitized_NoSpecialChars()
    {
        // SEC-07: slot names with special chars must be sanitized
        var t    = ProductTemplate.Create(TenantId, "door", "T").Value;
        var root = t.AddSlot("Root", "Root", null, null, 1, true, null, 0).Value;
        var evil = t.AddSlot("Panel<Script>!", "Panel", "MDF", 18m, 1, false, null, 1).Value;
        t.AddConnection(root.Id, evil.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);

        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        var plan   = _service.DeriveCncPlan(result);

        plan.Should().HaveCount(1);
        plan[0].SlotName.Should().NotContain("<").And.NotContain(">").And.NotContain("!");
        plan[0].SlotName.Should().Contain("Panel"); // safe chars preserved
    }

    [Fact]
    public void GrooveParameters_CorrectlyPropagated()
    {
        // TemplateBuilder sets grooveDepth=5, grooveWidth=8 on Dado connections
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan       = _service.DeriveCncPlan(result);
        var grooveOp   = plan.First(o => o.Operation == MachiningOperation.Groove);

        grooveOp.GrooveDepth.Should().Be(5m);
        grooveOp.GrooveWidth.Should().Be(8m);
    }

    [Fact]
    public void DrillParameters_CorrectlyPropagated()
    {
        var t      = DrillTemplate();
        var result = _engine.Calculate(t, new DimensionInput(600, 1800, 40));

        var plan = _service.DeriveCncPlan(result);

        plan.Should().HaveCount(1);
        plan[0].Operation.Should().Be(MachiningOperation.Drill);
        plan[0].DrillDiameter.Should().Be(8m);
        plan[0].DrillDepth.Should().Be(35m);
    }

    [Fact]
    public void FafTTemplate_CncPlan_ContainsGrooveAndEdgeBandOperations()
    {
        var t      = TemplateBuilder.BuildFafT();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveCncPlan(result);

        plan.Should().Contain(o => o.Operation == MachiningOperation.Groove,
            "FAF_T has Dado/Groove connections on FrameCore-Alap");
        plan.Should().Contain(o => o.Operation == MachiningOperation.EdgeBand,
            "FAF_T has EdgeBand connections on Ajtó-Él-V and Ajtó-Él-F");
    }
}
