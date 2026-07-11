using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.UseOffcutInJob;

public sealed class UseOffcutInJobCommandHandler
    : IRequestHandler<UseOffcutInJobCommand, Result<UseOffcutInJobResponse>>
{
    private readonly IInventoryRepository _repository;

    public UseOffcutInJobCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UseOffcutInJobResponse>> Handle(
        UseOffcutInJobCommand request, CancellationToken ct)
    {
        var offcut = await _repository.GetOffcutByIdAsync(request.OffcutId, ct).ConfigureAwait(false);
        if (offcut is null)
            return Result<UseOffcutInJobResponse>.NotFound($"Offcut {request.OffcutId} not found.");

        if (offcut.Status != OffcutStatus.Reserved)
            return Result<UseOffcutInJobResponse>.Conflict(
                $"Offcut must be Reserved before use (status: {offcut.Status}).");

        offcut.MarkUsed(request.JobId);

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<UseOffcutInJobResponse>.Success(
            new UseOffcutInJobResponse("Used", offcut.UsedInJobId!.Value, offcut.UsedAt!.Value));
    }
}
