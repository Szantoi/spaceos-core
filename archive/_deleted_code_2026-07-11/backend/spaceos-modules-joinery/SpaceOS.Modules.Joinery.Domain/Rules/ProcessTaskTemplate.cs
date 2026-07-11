namespace SpaceOS.Modules.Joinery.Domain.Rules;

public class ProcessTaskTemplate
{
    public string TaskId { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Department { get; set; }
    public int UnitTimeSec { get; set; }
    public int Headcount { get; set; }
    public string? ParentTaskId { get; set; }
}
