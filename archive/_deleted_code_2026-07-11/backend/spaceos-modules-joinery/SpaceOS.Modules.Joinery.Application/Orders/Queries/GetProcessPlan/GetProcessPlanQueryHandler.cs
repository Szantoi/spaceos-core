using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProcessPlan;

public sealed class GetProcessPlanQueryHandler : IRequestHandler<GetProcessPlanQuery, Result<ProcessPlanResponse>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IDoorRulesRepository _rulesRepository;
    private readonly IProcessFlowService _processFlowService;

    public GetProcessPlanQueryHandler(
        IDoorOrderRepository repository,
        IDoorRulesRepository rulesRepository,
        IProcessFlowService processFlowService)
    {
        _repository = repository;
        _rulesRepository = rulesRepository;
        _processFlowService = processFlowService;
    }

    public async Task<Result<ProcessPlanResponse>> Handle(GetProcessPlanQuery request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<ProcessPlanResponse>.NotFound($"DoorOrder {request.OrderId} not found.");

        var templates = await _rulesRepository.GetProcessTaskTemplatesAsync(ct).ConfigureAwait(false);
        var tasks = _processFlowService.GenerateProcessPlan(order, templates);

        return Result<ProcessPlanResponse>.Success(new ProcessPlanResponse(order.Id, tasks));
    }
}
