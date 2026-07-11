using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareList;

public sealed class GetHardwareListQueryHandler : IRequestHandler<GetHardwareListQuery, Result<HardwareListResponse>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IDoorRulesRepository _rulesRepository;
    private readonly IHardwareResolutionService _hardwareService;

    public GetHardwareListQueryHandler(
        IDoorOrderRepository repository,
        IDoorRulesRepository rulesRepository,
        IHardwareResolutionService hardwareService)
    {
        _repository = repository;
        _rulesRepository = rulesRepository;
        _hardwareService = hardwareService;
    }

    public async Task<Result<HardwareListResponse>> Handle(GetHardwareListQuery request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, request.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<HardwareListResponse>.NotFound($"DoorOrder {request.OrderId} not found.");

        var allItems = new List<HardwareListItem>();

        foreach (var item in order.Items)
        {
            var doorTypeStr = item.DoorType.ToString();
            var rule = await _rulesRepository.GetDoorTypeRuleAsync(doorTypeStr, ct).ConfigureAwait(false);
            if (rule is null)
                return Result<HardwareListResponse>.Error($"No DoorTypeRule found for '{doorTypeStr}'.");

            var hardware = _hardwareService.Resolve(item, rule);
            allItems.AddRange(hardware);
        }

        return Result<HardwareListResponse>.Success(new HardwareListResponse(order.Id, allItems.AsReadOnly()));
    }
}
