using System.Linq.Expressions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;

namespace SpaceOS.Modules.Inventory.Domain.Specifications;

/// <summary>
/// Filters Active reservations whose TTL has elapsed. Used by the expiry worker.
/// </summary>
public static class ExpiredActiveReservationsSpec
{
    /// <summary>Returns an expression matching Active reservations past their expiry time.</summary>
    public static Expression<Func<Reservation, bool>> Active()
        => r => r.Status == ReservationStatus.Active && r.ExpiresAt < DateTimeOffset.UtcNow;
}
