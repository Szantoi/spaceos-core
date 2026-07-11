using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcutStatsSummary;

public record GetOffcutStatsSummaryQuery : IRequest<Result<GetOffcutStatsSummaryResponse>>;

public record GetOffcutStatsSummaryResponse(
    decimal TotalAvailableVolumeM3,
    decimal TotalAvailableWeightKg,
    IReadOnlyDictionary<string, MaterialOffcutStats> AvailableByMaterial,
    int ReservedCount,
    int UsedCount,
    int ScrappedCount);

public record MaterialOffcutStats(decimal VolumeM3, decimal WeightKg);
