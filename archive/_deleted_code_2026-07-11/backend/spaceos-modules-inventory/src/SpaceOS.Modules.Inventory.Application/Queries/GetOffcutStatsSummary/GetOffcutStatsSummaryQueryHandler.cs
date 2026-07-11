using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcutStatsSummary;

public sealed class GetOffcutStatsSummaryQueryHandler
    : IRequestHandler<GetOffcutStatsSummaryQuery, Result<GetOffcutStatsSummaryResponse>>
{
    private readonly IInventoryRepository _repository;

    public GetOffcutStatsSummaryQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetOffcutStatsSummaryResponse>> Handle(
        GetOffcutStatsSummaryQuery request, CancellationToken ct)
    {
        var all = await _repository.GetAllOffcutsAsync(ct).ConfigureAwait(false);

        var available = all.Where(o => o.Status == OffcutStatus.Available).ToList();

        var byMaterial = available
            .GroupBy(o => o.MaterialCode)
            .ToDictionary(
                g => g.Key,
                g => new MaterialOffcutStats(
                    g.Sum(o => o.VolumeM3),
                    g.Sum(o => o.WeightKg)));

        return Result<GetOffcutStatsSummaryResponse>.Success(new GetOffcutStatsSummaryResponse(
            TotalAvailableVolumeM3: available.Sum(o => o.VolumeM3),
            TotalAvailableWeightKg: available.Sum(o => o.WeightKg),
            AvailableByMaterial: byMaterial,
            ReservedCount:  all.Count(o => o.Status == OffcutStatus.Reserved),
            UsedCount:      all.Count(o => o.Status == OffcutStatus.Used),
            ScrappedCount:  all.Count(o => o.Status == OffcutStatus.Scrapped)));
    }
}
