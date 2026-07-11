namespace SpaceOS.Modules.Joinery.Domain.Rules;

public class CuttingConstant
{
    public Guid Id { get; set; }
    public string DoorType { get; set; } = string.Empty;
    public string ComponentSlot { get; set; } = string.Empty;
    public string? FrameMaterialH { get; set; }
    public string? FrameMaterialV { get; set; }
    public decimal? FrameThicknessH { get; set; }
    public decimal? FrameThicknessV { get; set; }
    public decimal? FrameWidthOffsetH { get; set; }
    public decimal? FrameWidthOffsetV { get; set; }
    public decimal? FrameLengthOffsetH { get; set; }
    public decimal? FrameLengthOffsetV { get; set; }
    public int? FrameCountH { get; set; }
    public int? FrameCountV { get; set; }
}
