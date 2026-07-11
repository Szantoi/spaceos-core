using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcutList;

public sealed class GetOffcutListQueryHandler
    : IRequestHandler<GetOffcutListQuery, Result<GetOffcutListResponse>>
{
    private readonly IInventoryRepository _repository;

    public GetOffcutListQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetOffcutListResponse>> Handle(GetOffcutListQuery request, CancellationToken ct)
    {
        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _repository
            .GetOffcutPagedAsync(request.Status, request.MaterialCode, request.MinVolumeM3,
                                 request.CreatedAfter, page, pageSize, ct)
            .ConfigureAwait(false);

        var dtos = items.Select(o => new OffcutListItem(
            o.Id, o.MaterialCode, o.WidthMm, o.HeightMm, o.ThicknessMm,
            o.VolumeM3, o.WeightKg, o.Status.ToString(), o.CreatedAt, o.CuttingJobId)).ToList();

        return Result<GetOffcutListResponse>.Success(new GetOffcutListResponse(dtos, total, page, pageSize));
    }
}
