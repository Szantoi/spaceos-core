using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Inventory.Domain.Interfaces;

namespace SpaceOS.Modules.Inventory.Application.Commands.ApproveOffcutReservation;

public sealed class ApproveOffcutReservationCommandHandler
    : IRequestHandler<ApproveOffcutReservationCommand, Result<ApproveOffcutReservationResponse>>
{
    private readonly IInventoryRepository _repository;

    public ApproveOffcutReservationCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ApproveOffcutReservationResponse>> Handle(
        ApproveOffcutReservationCommand request, CancellationToken ct)
    {
        var reservation = await _repository
            .GetOffcutReservationByIdAsync(request.ReservationId, ct)
            .ConfigureAwait(false);

        if (reservation is null)
            return Result<ApproveOffcutReservationResponse>.NotFound(
                $"Reservation {request.ReservationId} not found.");

        if (reservation.IsExpired)
            return Result<ApproveOffcutReservationResponse>.Error("Reservation has expired.");

        reservation.Approve();

        var offcut = await _repository.GetOffcutByIdAsync(reservation.OffcutId, ct).ConfigureAwait(false);
        if (offcut is null)
            return Result<ApproveOffcutReservationResponse>.NotFound(
                $"Offcut {reservation.OffcutId} not found.");

        offcut.Reserve();

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<ApproveOffcutReservationResponse>.Success(
            new ApproveOffcutReservationResponse("Approved"));
    }
}
