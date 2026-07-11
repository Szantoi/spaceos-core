namespace SpaceOS.Modules.Joinery.Domain.Rules;

public class PartDimensionRule
{
    public Guid Id { get; set; }
    public string DoorType { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string? Material { get; set; }
    public decimal? Thickness { get; set; }
    public int Quantity { get; set; }
    public decimal WidthBase { get; set; }
    public decimal WidthMultiplierFactor { get; set; }
    public decimal LengthBase { get; set; }
    public decimal LengthMultiplierFactor { get; set; }
}
