using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordOffcut;

public sealed class RecordOffcutCommandHandler : IRequestHandler<RecordOffcutCommand, Result>
{
    private readonly IInventoryRepository _repository;

    public RecordOffcutCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RecordOffcutCommand request, CancellationToken ct)
    {
        var catalog = await _repository.GetMaterialCatalogByTypeAsync(request.MaterialType, ct).ConfigureAwait(false);
        if (catalog is null)
            return Result.NotFound($"Material type '{request.MaterialType}' not found.");

        var offcut = Offcut.Register(
            request.TenantId,
            catalog.Id,
            request.WidthMm,
            request.HeightMm,
            request.OriginCuttingSheetId);

        await _repository.AddOffcutAsync(offcut, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
