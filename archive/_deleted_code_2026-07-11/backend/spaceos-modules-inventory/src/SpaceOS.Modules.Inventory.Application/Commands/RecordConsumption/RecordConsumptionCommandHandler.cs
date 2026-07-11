using System.Text.Json;
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;

public sealed class RecordConsumptionCommandHandler : IRequestHandler<RecordConsumptionCommand, Result>
{
    private readonly IInventoryRepository _repository;

    public RecordConsumptionCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RecordConsumptionCommand request, CancellationToken ct)
    {
        var catalog = await _repository.GetMaterialCatalogByTypeAsync(request.MaterialType, ct).ConfigureAwait(false);
        if (catalog is null)
            return Result.NotFound($"Material type '{request.MaterialType}' not found.");

        var movement = StockMovement.Record(
            request.TenantId,
            MovementType.Consumption,
            catalog.Id,
            request.Area,
            request.OccurredAt,
            request.Reason);

        await _repository.AddStockMovementAsync(movement, ct).ConfigureAwait(false);

        // Reorder alert: check current stock level against ReorderPoint.
        // Both the movement record and the outbox row are saved in the same SaveChangesAsync call.
        var totalStock = await _repository.GetTotalStockQuantityAsync(request.TenantId, catalog.Id, ct).ConfigureAwait(false);
        if (totalStock <= catalog.ReorderPoint)
        {
            var payload = JsonSerializer.Serialize(new
            {
                tenantId = request.TenantId,
                materialCode = catalog.MaterialType,
                currentStock = (decimal)totalStock,
                reorderPoint = (decimal)catalog.ReorderPoint,
                suggestedQuantity = (decimal)catalog.SuggestedOrderQuantity,
                preferredSupplierId = catalog.PreferredSupplierId,
                unitOfMeasure = catalog.UnitOfMeasure,
                alertedAt = DateTimeOffset.UtcNow
            });
            var outbox = InventoryReorderOutbox.Create(request.TenantId, payload);
            await _repository.AddReorderOutboxAsync(outbox, ct).ConfigureAwait(false);
        }

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
