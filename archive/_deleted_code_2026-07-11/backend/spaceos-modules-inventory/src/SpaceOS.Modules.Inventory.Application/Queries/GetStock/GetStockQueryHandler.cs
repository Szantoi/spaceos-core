using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetStock;

public sealed class GetStockQueryHandler : IRequestHandler<GetStockQuery, Result<StockLevelResponse>>
{
    private readonly IInventoryRepository _repository;

    public GetStockQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StockLevelResponse>> Handle(GetStockQuery request, CancellationToken ct)
    {
        var catalog = await _repository.GetMaterialCatalogByTypeAsync(request.MaterialType, ct).ConfigureAwait(false);
        if (catalog is null)
            return Result<StockLevelResponse>.NotFound($"Material type '{request.MaterialType}' not found.");

        var stocks = await _repository.GetStockByMaterialTypeAsync(request.MaterialType, ct).ConfigureAwait(false);
        var fullPanelCount = stocks.Where(s => s.StockType == StockType.FullPanel).Sum(s => s.Quantity);
        var offcutCount = stocks.Where(s => s.StockType == StockType.Offcut).Sum(s => s.Quantity);

        return Result<StockLevelResponse>.Success(new StockLevelResponse(
            catalog.MaterialType,
            fullPanelCount,
            (int)catalog.StandardWidth,
            (int)catalog.StandardHeight,
            offcutCount));
    }
}
