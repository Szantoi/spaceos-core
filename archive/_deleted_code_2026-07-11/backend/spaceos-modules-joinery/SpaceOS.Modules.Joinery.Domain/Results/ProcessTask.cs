namespace SpaceOS.Modules.Joinery.Domain.Results;

public sealed record ProcessTask(
    string TaskId,
    string ShortName,
    string? Description,
    string? Department,
    TimeSpan UnitTime,
    int Headcount,
    string? ParentTaskId);
