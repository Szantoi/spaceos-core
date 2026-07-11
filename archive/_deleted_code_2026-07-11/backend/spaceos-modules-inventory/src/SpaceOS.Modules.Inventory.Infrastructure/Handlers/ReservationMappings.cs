using System.Text.Json;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Extension methods that map domain aggregates to contract DTOs.
/// </summary>
internal static class ReservationMappings
{
    /// <summary>Maps a <see cref="Reservation"/> aggregate to a <see cref="ReservationDto"/>.</summary>
    internal static ReservationDto ToReservationDto(this Reservation r)
    {
        JsonDocument? contextDoc = null;
        if (r.ConsumerContextJson is not null)
        {
            try { contextDoc = JsonDocument.Parse(r.ConsumerContextJson); }
            catch (JsonException) { /* keep null — caller validated on write */ }
        }

        var items = r.Items
            .Select(i => new ReservationItemDto(
                i.Id,
                i.StockItemId,
                i.MaterialCode,
                i.QuantityReserved,
                i.QuantityConsumed))
            .ToList();

        return new ReservationDto(
            r.Id,
            r.TenantId,
            r.CorrelationId,
            r.ConsumerModule,
            contextDoc,
            r.CreatedByUserId,
            r.CreatedAt,
            r.ExpiresAt,
            (SpaceOS.Modules.Contracts.Inventory.Enums.ReservationStatus)(int)r.Status,
            items);
    }
}
