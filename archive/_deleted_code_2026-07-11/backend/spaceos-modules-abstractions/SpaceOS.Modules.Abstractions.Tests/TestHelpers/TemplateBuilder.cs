using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Tests.TestHelpers;

/// <summary>
/// Shared factory helpers for building test templates.
/// </summary>
internal static class TemplateBuilder
{
    private static readonly Guid TestTenantId = new("b2c3d4e5-f6a7-8901-bcde-f01234567891");

    /// <summary>
    /// Builds the full FAF_T door template with 6 slots and all connections.
    /// CuttingOversize is set to 1.0.
    /// </summary>
    internal static ProductTemplate BuildFafT()
    {
        var t = ProductTemplate.Create(TestTenantId, "door", "FAF_T").Value;
        t.SetParameter("CuttingOversize", 1.0m);

        var root          = t.AddSlot("Root",           "Root",      null,    null, 1, true,  null, 0).Value;
        var bkmPanel      = t.AddSlot("BKM-panel",      "Panel",     "MDF",   18m,  1, false, null, 1).Value;
        var ajtolap       = t.AddSlot("Ajtólap",        "Door",      "MDF",   18m,  1, false, null, 2).Value;
        var frameCoreAlap = t.AddSlot("FrameCore-Alap", "FrameCore", "Solid", 40m,  2, false, null, 3).Value;
        var ajtoElV       = t.AddSlot("Ajtó-Él-V",      "Edge",      "ABS",   2m,   2, false, null, 4).Value;
        var ajtoElF       = t.AddSlot("Ajtó-Él-F",      "Edge",      "ABS",   2m,   2, false, null, 5).Value;

        // root → bkmPanel: Identity W + H, Offset/None/Design
        t.AddConnection(root.Id, bkmPanel.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, bkmPanel.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);

        // bkmPanel → ajtolap: Identity W / Subtract 6 H, Butt/Cut/Cutting
        t.AddConnection(bkmPanel.Id, ajtolap.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(bkmPanel.Id, ajtolap.Id, DimensionAxis.Height, RuleOperator.Subtract, 6m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);

        // ajtolap → frameCoreAlap: Dado/Groove/CNC with groove params
        t.AddConnection(ajtolap.Id, frameCoreAlap.Id, DimensionAxis.Width,  RuleOperator.Subtract, 8m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC,
            grooveDepth: 5m, grooveWidth: 8m);
        t.AddConnection(ajtolap.Id, frameCoreAlap.Id, DimensionAxis.Height, RuleOperator.Subtract, 4m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC,
            grooveDepth: 5m, grooveWidth: 8m);

        // ajtolap → ajtoElV: EdgeBand/EdgeBanding on H and Depth
        t.AddConnection(ajtolap.Id, ajtoElV.Id, DimensionAxis.Height, RuleOperator.Identity,  0m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtolap.Id, ajtoElV.Id, DimensionAxis.Depth,  RuleOperator.Constant,  2m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        // ajtolap → ajtoElF: EdgeBand/EdgeBanding on W and Depth
        t.AddConnection(ajtolap.Id, ajtoElF.Id, DimensionAxis.Width, RuleOperator.Identity, 0m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtolap.Id, ajtoElF.Id, DimensionAxis.Depth, RuleOperator.Constant, 2m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        return t;
    }
}
