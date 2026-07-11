namespace SpaceOS.Modules.Joinery.Domain.Rules;

public class DoorTypeRule
{
    public string DoorType { get; set; } = string.Empty;
    public int AjtólapCount { get; set; }
    public decimal BkmWidthFixed { get; set; }
    public decimal BkmHeightFixed { get; set; }
    public decimal BkmWidthMoving { get; set; }
    public decimal BkmHeightMoving { get; set; }
}
