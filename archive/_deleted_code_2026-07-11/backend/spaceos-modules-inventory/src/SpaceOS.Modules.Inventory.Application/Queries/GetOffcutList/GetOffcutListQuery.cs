using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcutList;

public record GetOffcutListQuery(
    string? Status,
    string? MaterialCode,
    decimal? MinVolumeM3,
    DateTime? CreatedAfter,
    int Page,
    int PageSize) : IRequest<Result<GetOffcutListResponse>>;

public record GetOffcutListResponse(
    IReadOnlyList<OffcutListItem> Offcuts,
    int Total,
    int Page,
    int PageSize);

public record OffcutListItem(
    Guid Id,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    decimal VolumeM3,
    decimal WeightKg,
    string Status,
    DateTime CreatedAt,
    Guid? CuttingJobId);
