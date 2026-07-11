using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProcessPlan;

public sealed record ProcessPlanResponse(
    Guid OrderId,
    IReadOnlyList<ProcessTask> Tasks);
