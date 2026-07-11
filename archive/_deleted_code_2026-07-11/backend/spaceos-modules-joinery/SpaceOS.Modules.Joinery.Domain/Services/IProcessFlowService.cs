using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Domain.Services;

public interface IProcessFlowService
{
    IReadOnlyList<ProcessTask> GenerateProcessPlan(DoorOrder order, IReadOnlyList<ProcessTaskTemplate> templates);
}
