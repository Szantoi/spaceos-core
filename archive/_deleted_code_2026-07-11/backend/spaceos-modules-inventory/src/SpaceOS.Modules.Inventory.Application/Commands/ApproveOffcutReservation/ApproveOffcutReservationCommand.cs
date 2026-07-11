using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.ApproveOffcutReservation;

public record ApproveOffcutReservationCommand(Guid ReservationId) : IRequest<Result<ApproveOffcutReservationResponse>>;

public record ApproveOffcutReservationResponse(string Status);
