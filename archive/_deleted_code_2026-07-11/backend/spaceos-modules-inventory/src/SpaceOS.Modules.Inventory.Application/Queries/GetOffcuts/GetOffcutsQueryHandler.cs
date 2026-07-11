using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcuts;

public sealed class GetOffcutsQueryHandler : IRequestHandler<GetOffcutsQuery, Result<IReadOnlyList<OffcutResponse>>>
{
    private readonly IInventoryRepository _repository;

    public GetOffcutsQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<OffcutResponse>>> Handle(GetOffcutsQuery request, CancellationToken ct)
    {
        var offcuts = await _repository.GetAvailableOffcutsByMaterialTypeAsync(request.MaterialType, ct).ConfigureAwait(false);
        var result = offcuts
            .Select(o => new OffcutResponse(o.Id, o.WidthMm, o.HeightMm, o.MaterialCatalogId, o.OriginCuttingSheetId))
            .ToList();
        return Result<IReadOnlyList<OffcutResponse>>.Success(result);
    }
}
