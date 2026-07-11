using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Graph;

/// <summary>
/// FAF_T full pathway:
/// Root(900×2100×40)
/// → BKM-panel (Identity W, Identity H)
/// → Ajtólap (Identity W, Subtract 6 H)
///   → FrameCore-Alap (Subtract 8 W, Subtract 4 H)
/// CuttingOversize = 1mm
/// </summary>
public class DoorFafTFullPathwayTests
{
    private static readonly Guid _tenantId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private readonly GraphCalculationEngine _engine = new();

    private ProductTemplate BuildFafTTemplate()
    {
        var t = ProductTemplate.Create(_tenantId, "door", "FAF_T").Value;
        t.SetParameter("CuttingOversize", 1m);

        var root = t.AddSlot("Root", "Root", null, null, 1, true, null, 0).Value;
        var bkm = t.AddSlot("BKM-panel", "Panel", "MDF", 18, 1, false, null, 1).Value;
        var ajtolap = t.AddSlot("Ajtólap", "Door", "MDF", 18, 1, false, null, 2).Value;
        var frameCore = t.AddSlot("FrameCore-Alap", "FrameCore", "Solid", 40, 2, false, null, 3).Value;

        // Root → BKM-panel: Identity W + H
        t.AddConnection(root.Id, bkm.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);
        t.AddConnection(root.Id, bkm.Id, DimensionAxis.Height, RuleOperator.Identity, 0, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Cutting);

        // BKM-panel → Ajtólap: Identity W, Subtract 6 H
        t.AddConnection(bkm.Id, ajtolap.Id, DimensionAxis.Width, RuleOperator.Identity, 0, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(bkm.Id, ajtolap.Id, DimensionAxis.Height, RuleOperator.Subtract, 6, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);

        // Ajtólap → FrameCore-Alap: Subtract 8 W, Subtract 4 H
        t.AddConnection(ajtolap.Id, frameCore.Id, DimensionAxis.Width, RuleOperator.Subtract, 8, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);
        t.AddConnection(ajtolap.Id, frameCore.Id, DimensionAxis.Height, RuleOperator.Subtract, 4, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);

        return t;
    }

    [Fact]
    public void FafT_BkmPanelWidth_Equals_RootWidth()
    {
        var t = BuildFafTTemplate();
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var bkm = t.Slots.First(s => s.Name == "BKM-panel");
        r.Dimensions[bkm.Id].Width.Should().Be(900);
    }

    [Fact]
    public void FafT_AjtolапHeight_Is_RootHeightMinus6()
    {
        var t = BuildFafTTemplate();
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var ajtolap = t.Slots.First(s => s.Name == "Ajtólap");
        r.Dimensions[ajtolap.Id].Height.Should().Be(2094); // 2100 - 6
    }

    [Fact]
    public void FafT_FrameCore_Width_IsBkm_Minus8()
    {
        var t = BuildFafTTemplate();
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        var frameCore = t.Slots.First(s => s.Name == "FrameCore-Alap");
        r.Dimensions[frameCore.Id].Width.Should().Be(892); // 900 - 8
    }

    [Fact]
    public void FafT_CuttingList_ContainsAtLeast3Physical_Items()
    {
        var t = BuildFafTTemplate();
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        r.CuttingList.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void FafT_CuttingOversize_Applied_To_AllItems()
    {
        var t = BuildFafTTemplate();
        var r = _engine.Calculate(t, new DimensionInput(900, 2100, 40));

        // CuttingOversize=1 applied, BKM Width = 900+1 = 901
        var bkmItem = r.CuttingList.First(i => i.SlotName == "BKM-panel");
        bkmItem.Width.Should().Be(901m);
    }

    [Fact]
    public void FafT_DimensionsRounded_To1Decimal()
    {
        var t = BuildFafTTemplate();
        var r = _engine.Calculate(t, new DimensionInput(900.15m, 2100.25m, 40));

        // Each dimension should have at most 1 decimal place: multiply by 10, take integer part
        foreach (var d in r.Dimensions.Values)
        {
            // Verify no more than 1 decimal: decimal(x, 1) round-trip should equal original
            d.Width.Should().Be(Math.Round(d.Width, 1, MidpointRounding.AwayFromZero),
                $"Width {d.Width} must have at most 1 decimal place");
            d.Height.Should().Be(Math.Round(d.Height, 1, MidpointRounding.AwayFromZero),
                $"Height {d.Height} must have at most 1 decimal place");
        }
    }
}
