using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CalculateDoorOrder;

public sealed class CalculateDoorOrderCommandHandler : IRequestHandler<CalculateDoorOrderCommand, Result<CuttingListResponse>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IDoorRulesRepository _rulesRepository;
    private readonly IDoorCalculationService _calculationService;
    private readonly IMediator _mediator;

    public CalculateDoorOrderCommandHandler(
        IDoorOrderRepository repository,
        IDoorRulesRepository rulesRepository,
        IDoorCalculationService calculationService,
        IMediator mediator)
    {
        _repository = repository;
        _rulesRepository = rulesRepository;
        _calculationService = calculationService;
        _mediator = mediator;
    }

    public async Task<Result<CuttingListResponse>> Handle(CalculateDoorOrderCommand request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<CuttingListResponse>.NotFound($"DoorOrder {request.OrderId} not found.");

        var allItems = new List<CuttingListItem>();

        foreach (var item in order.Items)
        {
            var doorTypeStr = item.DoorType.ToString();
            var rule = await _rulesRepository.GetDoorTypeRuleAsync(doorTypeStr, ct).ConfigureAwait(false);
            var dimRules = await _rulesRepository.GetPartDimensionRulesAsync(doorTypeStr, ct).ConfigureAwait(false);
            var constants = await _rulesRepository.GetGlobalConstantsAsync(ct).ConfigureAwait(false);

            if (rule is null)
                return Result<CuttingListResponse>.Error($"No DoorTypeRule found for '{doorTypeStr}'.");

            var cuttingItems = _calculationService.CalculateCuttingList(item, rule, dimRules, constants.FirstOrDefault()!);
            allItems.AddRange(cuttingItems);
        }

        var domainEvent = new DoorOrderCalculated(order.Id, order.TenantId, order.Items.Count);
        await DomainEventDispatcher.DispatchAsync(_mediator, [domainEvent], ct).ConfigureAwait(false);

        var response = new CuttingListResponse(order.Id, allItems.AsReadOnly(), order.Items.Count);
        return Result<CuttingListResponse>.Success(response);
    }
}
