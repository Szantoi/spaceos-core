using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Inventory.Application.Handlers;
using SpaceOS.Modules.Inventory.Domain.Specifications;
using SpaceOS.Modules.Inventory.Infrastructure.Observability;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="ReleaseReservationCommand"/>.
/// </summary>
public sealed class ReleaseReservationCommandHandler : IRequestHandler<ReleaseReservationCommand, Result>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<ReleaseReservationCommandHandler> _logger;

    public ReleaseReservationCommandHandler(
        InventoryDbContext db,
        ILogger<ReleaseReservationCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(ReleaseReservationCommand request, CancellationToken ct)
    {
        // Find the active reservation with its items (need tracking for update)
        var reservation = await _db.Reservations
            .Include(r => r.Items)
            .Where(ReservationByCorrelationActiveSpec.For(request.TenantId, request.CorrelationId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (reservation is null)
            return Result.NotFound("No active reservation found for the given correlation ID.");

        reservation.Release(request.Reason);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        ReservationMetrics.ReservationsReleased.Add(1,
            new KeyValuePair<string, object?>("tenant_id", request.TenantId.ToString()));

        _logger.LogInformation(
            "ReleaseReservation: released reservation {ReservationId} for tenant {TenantId} / correlation {CorrelationId}",
            reservation.Id, request.TenantId, request.CorrelationId);

        return Result.Success();
    }
}
