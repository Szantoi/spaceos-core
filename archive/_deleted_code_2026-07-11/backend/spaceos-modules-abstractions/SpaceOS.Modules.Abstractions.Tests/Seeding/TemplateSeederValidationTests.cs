using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Infrastructure.Services;
using SpaceOS.Modules.Abstractions.Tests.TestHelpers;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Seeding;

public class TemplateSeederValidationTests
{
    private static readonly Guid TenantId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private readonly TemplateValidatorService _validator = new();

    [Fact]
    public void FafT_Template_ValidatorPasses()
    {
        var template = TemplateBuilder.BuildFafT();
        var result = _validator.Validate(template);
        result.IsSuccess.Should().BeTrue("FAF_T template must pass structural validation");
    }

    [Fact]
    public void FafU_Template_ValidatorPasses()
    {
        var t = ProductTemplate.Create(TenantId, "door", "FAF_Ü").Value;
        t.SetParameter("CuttingOversize", 1.0m);

        var root      = t.AddSlot("Root",           "Root",      null,    null, 1, true,  null, 0).Value;
        var bkmPanel  = t.AddSlot("BKM-panel",       "Panel",     "MDF",   18m,  1, false, null, 1).Value;
        var ajtoLap   = t.AddSlot("Üveges-Ajtólap",  "Door",      "MDF",   18m,  1, false, null, 2).Value;
        var uveg      = t.AddSlot("Üveg",             "Glass",     null,    6m,   1, false, null, 3).Value;
        var frameCore = t.AddSlot("FrameCore-Alap",   "FrameCore", "Solid", 40m,  2, false, null, 4).Value;
        var elV       = t.AddSlot("Ajtó-Él-V",        "Edge",      "ABS",   2m,   2, false, null, 5).Value;
        var elF       = t.AddSlot("Ajtó-Él-F",        "Edge",      "ABS",   2m,   2, false, null, 6).Value;

        t.AddConnection(root.Id, bkmPanel.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null, JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, bkmPanel.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null, JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(bkmPanel.Id, ajtoLap.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null, JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(bkmPanel.Id, ajtoLap.Id, DimensionAxis.Height, RuleOperator.Subtract, 6m, null, null, JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(ajtoLap.Id, uveg.Id, DimensionAxis.Width,  RuleOperator.Subtract, 120m, null, null, JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(ajtoLap.Id, uveg.Id, DimensionAxis.Height, RuleOperator.Subtract, 120m, null, null, JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(ajtoLap.Id, frameCore.Id, DimensionAxis.Width,  RuleOperator.Subtract, 8m,  null, null, JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);
        t.AddConnection(ajtoLap.Id, frameCore.Id, DimensionAxis.Height, RuleOperator.Subtract, 4m,  null, null, JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);
        t.AddConnection(ajtoLap.Id, elV.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtoLap.Id, elV.Id, DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtoLap.Id, elF.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtoLap.Id, elF.Id, DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        var result = _validator.Validate(t);
        result.IsSuccess.Should().BeTrue("FAF_Ü template must pass structural validation");
    }

    [Fact]
    public void Bfaj_Template_ValidatorPasses()
    {
        var t = ProductTemplate.Create(TenantId, "door", "BFAJ").Value;
        t.SetParameter("CuttingOversize", 1.0m);

        var root      = t.AddSlot("Root",           "Root",      null,    null, 1, true,  null, 0).Value;
        var ajtoLap   = t.AddSlot("Ajtólap",        "Door",      "MDF",   18m,  1, false, null, 1).Value;
        var frameCore = t.AddSlot("FrameCore-Alap", "FrameCore", "Solid", 40m,  2, false, null, 2).Value;
        var elV       = t.AddSlot("Ajtó-Él-V",      "Edge",      "ABS",   2m,   2, false, null, 3).Value;
        var elF       = t.AddSlot("Ajtó-Él-F",      "Edge",      "ABS",   2m,   2, false, null, 4).Value;

        t.AddConnection(root.Id, ajtoLap.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null, JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, ajtoLap.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null, JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(ajtoLap.Id, frameCore.Id, DimensionAxis.Width,  RuleOperator.Subtract, 8m, null, null, JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);
        t.AddConnection(ajtoLap.Id, frameCore.Id, DimensionAxis.Height, RuleOperator.Subtract, 4m, null, null, JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);
        t.AddConnection(ajtoLap.Id, elV.Id, DimensionAxis.Height, RuleOperator.Identity, 0m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtoLap.Id, elV.Id, DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtoLap.Id, elF.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        t.AddConnection(ajtoLap.Id, elF.Id, DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null, JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        var result = _validator.Validate(t);
        result.IsSuccess.Should().BeTrue("BFAJ template must pass structural validation");
    }

    [Fact]
    public void FafU_GlassSlot_HasCorrectTypeAndIsPhysical()
    {
        var t = ProductTemplate.Create(TenantId, "door", "FAF_Ü_Check").Value;
        var root      = t.AddSlot("Root",           "Root",      null,   null, 1, true,  null, 0).Value;
        var ajtoLap   = t.AddSlot("Üveges-Ajtólap", "Door",      "MDF",  18m,  1, false, null, 1).Value;
        var uveg      = t.AddSlot("Üveg",            "Glass",     null,   6m,   1, false, null, 2).Value;

        t.AddConnection(root.Id, ajtoLap.Id, DimensionAxis.Width,  RuleOperator.Identity, 0m,   null, null, JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(root.Id, ajtoLap.Id, DimensionAxis.Height, RuleOperator.Identity, 0m,   null, null, JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        t.AddConnection(ajtoLap.Id, uveg.Id, DimensionAxis.Width,  RuleOperator.Subtract, 120m, null, null, JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);
        t.AddConnection(ajtoLap.Id, uveg.Id, DimensionAxis.Height, RuleOperator.Subtract, 120m, null, null, JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);

        uveg.ComponentType.Should().Be("Glass", "the glass slot must use the Glass component type");
        uveg.IsVirtual.Should().BeFalse("glass is a physical component — it appears on the manufacturing sheet");
    }
}
