using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Manufacturing;

public class GlassOrderItemTests
{
    private static readonly Guid TenantId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private readonly GraphCalculationEngine _engine = new();
    private readonly ManufacturingDerivationService _service = new();

    private static ProductTemplate BuildTemplateWithGlass()
    {
        var t    = ProductTemplate.Create(TenantId, "door", "FAF_Ü_Test").Value;
        var root = t.AddSlot("Root",      "Root",  null,  null, 1, true,  null, 0).Value;
        var door = t.AddSlot("Ajtólap",   "Door",  "MDF", 18m,  1, false, null, 1).Value;
        var uveg = t.AddSlot("Üveg",      "Glass", null,  6m,   1, false, null, 2).Value;

        t.AddConnection(root.Id, door.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m,   null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, door.Id, DimensionAxis.Height, RuleOperator.Identity, 0m,   null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(door.Id, uveg.Id, DimensionAxis.Width,  RuleOperator.Subtract, 120m, null, null,
            JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(door.Id, uveg.Id, DimensionAxis.Height, RuleOperator.Subtract, 120m, null, null,
            JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);

        return t;
    }

    [Fact]
    public void GlassSlot_ExcludedFromCuttingList()
    {
        var t      = BuildTemplateWithGlass();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        result.CuttingList.Should().NotContain(i => i.ComponentType == "Glass",
            "Glass slots must not appear in the CNC cutting list");
    }

    [Fact]
    public void GlassSlot_AppearsInGlassOrderItems()
    {
        var t      = BuildTemplateWithGlass();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var orders = _service.DeriveGlassOrderItems(result);

        orders.Should().ContainSingle(o => o.SlotName == "Üveg",
            "Glass slot must appear in the order items list");
        orders[0].Width.Should().Be(780m,  "900 - 120 = 780");
        orders[0].Height.Should().Be(1980m, "2100 - 120 = 1980");
        orders[0].Quantity.Should().Be(1);
        orders[0].OrderDescription.Should().Contain("780").And.Contain("1980").And.Contain("1 db");
    }

    [Fact]
    public void GlassSlot_ExcludedFromCncPlan()
    {
        var t      = BuildTemplateWithGlass();
        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var plan = _service.DeriveCncPlan(result);

        plan.Should().NotContain(op => op.SlotName == "Üveg",
            "Glass slots must not appear in the CNC operation plan");
    }

    [Fact]
    public void NoGlassSlots_DeriveGlassOrderItems_ReturnsEmpty()
    {
        var t    = ProductTemplate.Create(TenantId, "door", "NoGlass").Value;
        var root = t.AddSlot("Root",  "Root",  null,  null, 1, true,  null, 0).Value;
        var door = t.AddSlot("Panel", "Panel", "MDF", 18m,  1, false, null, 1).Value;
        t.AddConnection(root.Id, door.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, door.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);

        var result = _engine.Calculate(t, new DimensionInput(900, 2100, 40));
        var orders = _service.DeriveGlassOrderItems(result);

        orders.Should().BeEmpty("no Glass slots in this template");
    }
}
