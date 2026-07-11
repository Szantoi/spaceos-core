using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Handlers;

/// <summary>
/// Releases an Active reservation identified by the (TenantId, CorrelationId) pair,
/// freeing the soft-reserved stock.
/// </summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="CorrelationId">Idempotency key that identifies the reservation.</param>
/// <param name="Reason">Optional free-text reason for audit.</param>
public sealed record ReleaseReservationCommand(
    Guid TenantId,
    Guid CorrelationId,
    string? Reason
) : IRequest<Result>;
