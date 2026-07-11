using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.RegisterOffcutBatch;

public sealed class RegisterOffcutBatchCommandHandler
    : IRequestHandler<RegisterOffcutBatchCommand, Result<RegisterOffcutBatchResponse>>
{
    private readonly IInventoryRepository _repo;

    public RegisterOffcutBatchCommandHandler(IInventoryRepository repo) => _repo = repo;

    public async Task<Result<RegisterOffcutBatchResponse>> Handle(
        RegisterOffcutBatchCommand command, CancellationToken ct)
    {
        if (command.Items is null || command.Items.Count == 0)
            return Result<RegisterOffcutBatchResponse>.Error("Items list cannot be empty.");

        // Idempotency check
        var existing = await _repo
            .GetOffcutBatchAsync(command.TenantId, command.SourceType, command.SourceId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var existingOffcuts = await _repo
                .GetOffcutsByOriginSheetIdAsync(command.TenantId, command.SourceId, ct)
                .ConfigureAwait(false);
            return Result<RegisterOffcutBatchResponse>.Success(
                new RegisterOffcutBatchResponse(existing.Id, existingOffcuts.Select(o => o.Id).ToList(), IsNew: false));
        }

        // New batch
        var batch = OffcutBatch.Create(command.TenantId, command.SourceType, command.SourceId);
        await _repo.AddOffcutBatchAsync(batch, ct).ConfigureAwait(false);

        var offcutIds = new List<Guid>();
        foreach (var item in command.Items)
        {
            var offcut = Offcut.Register(
                command.TenantId,
                item.MaterialCatalogId,
                item.MaterialCode,
                item.WidthMm,
                item.HeightMm,
                item.ThicknessMm,
                volumeM3: 0m,
                weightKg: 0m,
                originCuttingSheetId: command.SourceId,
                cuttingJobId: null);
            await _repo.AddOffcutAsync(offcut, ct).ConfigureAwait(false);
            offcutIds.Add(offcut.Id);
        }

        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<RegisterOffcutBatchResponse>.Success(
            new RegisterOffcutBatchResponse(batch.Id, offcutIds, IsNew: true));
    }
}
