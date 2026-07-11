namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

/// <summary>
/// A single line in a <see cref="Reservation"/> linking a stock item to its reserved quantity.
/// Not an aggregate — no domain events. Owned by <see cref="Reservation"/>.
/// </summary>
public class ReservationItem
{
    /// <summary>Surrogate PK.</summary>
    public Guid Id { get; private set; }

    /// <summary>Parent reservation FK.</summary>
    public Guid ReservationId { get; private set; }

    /// <summary>Denormalized tenant id (DB-02: avoids join for RLS checks).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>FK to the stock row being reserved.</summary>
    public Guid StockItemId { get; private set; }

    /// <summary>Human-readable material code; max 20 chars.</summary>
    public string MaterialCode { get; private set; } = string.Empty;

    /// <summary>Quantity locked by this reservation item.</summary>
    public decimal QuantityReserved { get; private set; }

    /// <summary>Quantity actually consumed against the reservation. Cannot exceed <see cref="QuantityReserved"/>.</summary>
    public decimal QuantityConsumed { get; private set; }

    private ReservationItem() { }

    /// <summary>
    /// Factory that validates all invariants and constructs a <see cref="ReservationItem"/>.
    /// </summary>
    public static ReservationItem Create(
        Guid reservationId,
        Guid tenantId,
        Guid stockItemId,
        string materialCode,
        decimal quantityReserved)
    {
        if (reservationId == Guid.Empty) throw new ArgumentException("ReservationId required.", nameof(reservationId));
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (stockItemId == Guid.Empty) throw new ArgumentException("StockItemId required.", nameof(stockItemId));
        if (string.IsNullOrWhiteSpace(materialCode)) throw new ArgumentException("MaterialCode required.", nameof(materialCode));
        if (materialCode.Length > 20) throw new ArgumentException("MaterialCode max 20 chars.", nameof(materialCode));
        // I-06: QuantityReserved > 0
        if (quantityReserved <= 0) throw new ArgumentException("QuantityReserved must be positive.", nameof(quantityReserved));

        return new ReservationItem
        {
            Id = Guid.NewGuid(),
            ReservationId = reservationId,
            TenantId = tenantId,
            StockItemId = stockItemId,
            MaterialCode = materialCode,
            QuantityReserved = quantityReserved,
            QuantityConsumed = 0m
        };
    }

    /// <summary>
    /// Records consumption of <paramref name="amount"/> units.
    /// Enforces I-07: <see cref="QuantityConsumed"/> cannot exceed <see cref="QuantityReserved"/>.
    /// </summary>
    public void RecordConsumption(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Consumption amount must be positive.", nameof(amount));
        if (QuantityConsumed + amount > QuantityReserved)
            throw new InvalidOperationException(
                $"Cannot consume {amount}: total consumed ({QuantityConsumed + amount}) would exceed reserved ({QuantityReserved}).");
        QuantityConsumed += amount;
    }
}
