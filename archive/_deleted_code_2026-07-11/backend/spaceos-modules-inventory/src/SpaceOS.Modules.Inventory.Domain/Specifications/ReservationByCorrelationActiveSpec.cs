using System.Linq.Expressions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;

namespace SpaceOS.Modules.Inventory.Domain.Specifications;

/// <summary>
/// Filters reservations to the single Active record for a given tenant + correlationId pair (BE-06).
/// Matches the partial unique index <c>ux_reservations_tenant_correlation_active</c> (WHERE status = 0).
/// </summary>
public static class ReservationByCorrelationActiveSpec
{
    /// <summary>Returns an expression that matches only Active reservations for the given tenant and correlation.</summary>
    public static Expression<Func<Reservation, bool>> For(Guid tenantId, Guid correlationId)
        => r => r.TenantId == tenantId
             && r.CorrelationId == correlationId
             && r.Status == ReservationStatus.Active;
}
