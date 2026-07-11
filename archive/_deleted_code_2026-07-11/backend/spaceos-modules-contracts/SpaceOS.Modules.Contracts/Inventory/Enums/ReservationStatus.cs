namespace SpaceOS.Modules.Contracts.Inventory.Enums;

/// <summary>Lifecycle status of a soft stock reservation.</summary>
public enum ReservationStatus
{
    /// <summary>Reservation is active and stock is held.</summary>
    Active = 0,

    /// <summary>Reservation was explicitly released by the consumer.</summary>
    Released = 1,

    /// <summary>Reservation expired because the TTL elapsed without consumption.</summary>
    Expired = 2,

    /// <summary>Reservation was fully consumed via RecordConsumptionAsync.</summary>
    Consumed = 3,
}
