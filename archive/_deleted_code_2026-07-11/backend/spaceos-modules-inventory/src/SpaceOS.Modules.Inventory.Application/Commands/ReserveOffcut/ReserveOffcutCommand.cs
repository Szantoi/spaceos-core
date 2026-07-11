using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.ReserveOffcut;

public record ReserveOffcutCommand(Guid OffcutId, Guid JobId, Guid TenantId)
    : IRequest<Result<ReserveOffcutResponse>>;

public record ReserveOffcutResponse(Guid ReservationId, DateTime ExpiresAt);
