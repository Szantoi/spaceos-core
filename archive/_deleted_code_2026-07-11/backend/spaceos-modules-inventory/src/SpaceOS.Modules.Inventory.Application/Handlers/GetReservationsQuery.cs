using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Contracts.Inventory.DTOs;

namespace SpaceOS.Modules.Inventory.Application.Handlers;

/// <summary>
/// Returns a paginated list of reservations for a tenant, filtered by the supplied criteria.
/// At least one filter field must be non-null (DoS guard).
/// </summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="Filter">Query filter and pagination parameters.</param>
public sealed record GetReservationsQuery(
    Guid TenantId,
    ReservationFilter Filter
) : IRequest<Result<IReadOnlyList<ReservationDto>>>;
