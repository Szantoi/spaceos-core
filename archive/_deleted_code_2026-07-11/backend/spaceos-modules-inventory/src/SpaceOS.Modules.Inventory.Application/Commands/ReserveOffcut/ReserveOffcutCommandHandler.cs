using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.ReserveOffcut;

public sealed class ReserveOffcutCommandHandler
    : IRequestHandler<ReserveOffcutCommand, Result<ReserveOffcutResponse>>
{
    private readonly IInventoryRepository _repository;

    public ReserveOffcutCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ReserveOffcutResponse>> Handle(
        ReserveOffcutCommand request, CancellationToken ct)
    {
        var offcut = await _repository.GetOffcutByIdAsync(request.OffcutId, ct).ConfigureAwait(false);
        if (offcut is null)
            return Result<ReserveOffcutResponse>.NotFound($"Offcut {request.OffcutId} not found.");

        if (offcut.Status != OffcutStatus.Available)
            return Result<ReserveOffcutResponse>.Conflict(
                $"Offcut is not available for reservation (status: {offcut.Status}).");

        var reservation = OffcutReservation.Create(request.OffcutId, request.JobId, request.TenantId);

        await _repository.AddOffcutReservationAsync(reservation, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<ReserveOffcutResponse>.Success(
            new ReserveOffcutResponse(reservation.Id, reservation.ExpiresAt));
    }
}
