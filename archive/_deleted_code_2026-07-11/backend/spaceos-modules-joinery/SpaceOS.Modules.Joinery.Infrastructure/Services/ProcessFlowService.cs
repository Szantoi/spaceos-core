using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Services;

public sealed class ProcessFlowService : IProcessFlowService
{
    public IReadOnlyList<ProcessTask> GenerateProcessPlan(DoorOrder order, IReadOnlyList<ProcessTaskTemplate> templates)
        => templates.Select(t => new ProcessTask(t.TaskId, t.ShortName, t.Description, t.Department,
            TimeSpan.FromSeconds(t.UnitTimeSec), t.Headcount, t.ParentTaskId)).ToList().AsReadOnly();
}
