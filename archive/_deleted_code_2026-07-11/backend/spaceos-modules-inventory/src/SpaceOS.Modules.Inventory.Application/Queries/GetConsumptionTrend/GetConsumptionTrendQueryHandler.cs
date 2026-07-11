using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetConsumptionTrend;

public sealed class GetConsumptionTrendQueryHandler
    : IRequestHandler<GetConsumptionTrendQuery, Result<ConsumptionTrendResponse>>
{
    private readonly IInventoryRepository _repository;

    public GetConsumptionTrendQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ConsumptionTrendResponse>> Handle(GetConsumptionTrendQuery request, CancellationToken ct)
    {
        var movements = await _repository.GetMovementsByMaterialTypeAndDateRangeAsync(
            request.MaterialType, request.From, request.To, ct).ConfigureAwait(false);

        var consumptions = movements
            .Where(m => m.MovementType == MovementType.Consumption)
            .GroupBy(m => m.OccurredAt.Date)
            .Select(g => new DailyConsumptionEntry(g.Key, g.Sum(m => m.Quantity)))
            .OrderBy(d => d.Date)
            .ToList();

        var average = consumptions.Count > 0 ? consumptions.Average(d => d.Area) : 0m;

        return Result<ConsumptionTrendResponse>.Success(new ConsumptionTrendResponse(
            request.MaterialType,
            consumptions,
            average));
    }
}
