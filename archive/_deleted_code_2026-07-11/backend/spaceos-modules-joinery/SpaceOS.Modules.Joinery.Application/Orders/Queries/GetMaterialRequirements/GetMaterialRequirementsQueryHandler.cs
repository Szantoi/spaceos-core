using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialRequirements;

public sealed class GetMaterialRequirementsQueryHandler
    : IRequestHandler<GetMaterialRequirementsQuery, Result<MaterialRequirementsResponse>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IDoorRulesRepository _rulesRepository;
    private readonly IDoorCalculationService _calculationService;
    private readonly IMaterialRequirementService _materialService;

    public GetMaterialRequirementsQueryHandler(
        IDoorOrderRepository repository,
        IDoorRulesRepository rulesRepository,
        IDoorCalculationService calculationService,
        IMaterialRequirementService materialService)
    {
        _repository = repository;
        _rulesRepository = rulesRepository;
        _calculationService = calculationService;
        _materialService = materialService;
    }

    public async Task<Result<MaterialRequirementsResponse>> Handle(
        GetMaterialRequirementsQuery request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<MaterialRequirementsResponse>.NotFound($"DoorOrder {request.OrderId} not found.");

        var allCuttingItems = new List<CuttingListItem>();

        foreach (var item in order.Items)
        {
            var doorTypeStr = item.DoorType.ToString();
            var rule = await _rulesRepository.GetDoorTypeRuleAsync(doorTypeStr, ct).ConfigureAwait(false);
            var dimRules = await _rulesRepository.GetPartDimensionRulesAsync(doorTypeStr, ct).ConfigureAwait(false);
            var constants = await _rulesRepository.GetGlobalConstantsAsync(ct).ConfigureAwait(false);

            if (rule is null)
                return Result<MaterialRequirementsResponse>.Error($"No DoorTypeRule found for '{doorTypeStr}'.");

            var cuttingItems = _calculationService.CalculateCuttingList(item, rule, dimRules, constants.FirstOrDefault()!);
            allCuttingItems.AddRange(cuttingItems);
        }

        var requirements = _materialService.Calculate(allCuttingItems.AsReadOnly());

        return Result<MaterialRequirementsResponse>.Success(
            new MaterialRequirementsResponse(order.Id, requirements));
    }
}
