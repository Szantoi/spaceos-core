namespace SpaceOS.Modules.Inventory.Domain.Enums;

/// <summary>Lifecycle states of a stock reservation.</summary>
public enum ReservationStatus
{
    Active = 0,
    Released = 1,
    Expired = 2,
    Consumed = 3
}
