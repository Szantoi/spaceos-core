using System.Linq.Expressions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Domain.Specifications;

/// <summary>
/// Filters reservations by tenant + correlationId regardless of status.
/// Used when loading a reservation with its items for read or mutation.
/// </summary>
public static class ReservationWithItemsSpec
{
    /// <summary>Returns an expression that matches any reservation for the given tenant and correlation id.</summary>
    public static Expression<Func<Reservation, bool>> For(Guid tenantId, Guid correlationId)
        => r => r.TenantId == tenantId && r.CorrelationId == correlationId;
}
