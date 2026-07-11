using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordInbound;

public sealed class RecordInboundCommandHandler : IRequestHandler<RecordInboundCommand, Result>
{
    private readonly IInventoryRepository _repository;

    public RecordInboundCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RecordInboundCommand request, CancellationToken ct)
    {
        var catalog = await _repository.GetMaterialCatalogByTypeAsync(request.MaterialType, ct).ConfigureAwait(false);
        if (catalog is null)
            return Result.NotFound($"Material type '{request.MaterialType}' not found.");

        var movement = StockMovement.Record(
            request.TenantId,
            MovementType.Inbound,
            catalog.Id,
            request.Area,
            request.OccurredAt,
            request.Reference);

        await _repository.AddStockMovementAsync(movement, ct).ConfigureAwait(false);

        var stock = PanelStock.Create(
            request.TenantId,
            catalog.Id,
            catalog.StandardWidth,
            catalog.StandardHeight,
            StockType.FullPanel,
            request.PanelCount,
            "INBOUND");

        await _repository.AddPanelStockAsync(stock, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
