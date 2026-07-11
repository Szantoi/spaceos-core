using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetCuttingList;

// SEC-05: Never cached — always computed fresh.
public sealed class GetCuttingListQueryHandler : IRequestHandler<GetCuttingListQuery, Result<CuttingListResponse>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IDoorRulesRepository _rulesRepository;
    private readonly IDoorCalculationService _calculationService;

    public GetCuttingListQueryHandler(
        IDoorOrderRepository repository,
        IDoorRulesRepository rulesRepository,
        IDoorCalculationService calculationService)
    {
        _repository = repository;
        _rulesRepository = rulesRepository;
        _calculationService = calculationService;
    }

    public async Task<Result<CuttingListResponse>> Handle(GetCuttingListQuery request, CancellationToken ct)
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

        var response = new CuttingListResponse(order.Id, allItems.AsReadOnly(), order.Items.Count);
        return Result<CuttingListResponse>.Success(response);
    }
}
