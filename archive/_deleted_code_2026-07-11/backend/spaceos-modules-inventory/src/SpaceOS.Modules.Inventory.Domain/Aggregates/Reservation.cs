using SpaceOS.Modules.Inventory.Domain.Common;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Events;

namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

/// <summary>
/// Aggregate root representing a time-bounded stock reservation for a consumer module.
/// Invariants I-01..I-12 are enforced here; database-level invariants (I-04, I-09) are also
/// supported by a partial unique index and the application-layer availability check.
/// </summary>
public class Reservation : AggregateRoot
{
    private static readonly TimeSpan MinTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxTtl = TimeSpan.FromHours(168);

    private readonly List<ReservationItem> _items = new();

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CorrelationId { get; private set; }

    /// <summary>Name of the consumer module (e.g. "Cutting", "Joinery"). Validated externally against IModuleRegistry.</summary>
    public string ConsumerModule { get; private set; } = string.Empty;

    /// <summary>Opaque JSON blob supplied by the consumer, stored as jsonb. Validated externally by ConsumerContextValidator.</summary>
    public string? ConsumerContextJson { get; private set; }

    /// <summary>Optional user who initiated the reservation (SEC-10).</summary>
    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public ReservationStatus Status { get; private set; }

    /// <summary>xmin-based optimistic concurrency token (Npgsql UseXminAsConcurrencyToken).</summary>
    public uint RowVersion { get; private set; }

    /// <summary>Read-only projection of owned reservation items.</summary>
    public IReadOnlyList<ReservationItem> Items => _items.AsReadOnly();

    private Reservation() { }

    /// <summary>
    /// Creates a new Active reservation. Enforces I-01, I-02, I-03, I-06, I-10.
    /// Raises <see cref="StockReservedDomainEvent"/>.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="correlationId">Caller-supplied idempotency key. Unique per tenant for Active reservations (I-04).</param>
    /// <param name="consumerModule">Allowed consumer module name (caller validates via IModuleRegistry — I-12).</param>
    /// <param name="contextJson">Optional JSON context blob (caller validates via ConsumerContextValidator — I-11).</param>
    /// <param name="createdByUserId">Optional user id for audit trail (SEC-10).</param>
    /// <param name="items">At least one item tuple (stockItemId, materialCode, quantity). I-01, I-06.</param>
    /// <param name="ttl">Time-to-live; must be between 1 h and 168 h (I-03).</param>
    public static Reservation Reserve(
        Guid tenantId,
        Guid correlationId,
        string consumerModule,
        string? contextJson,
        Guid? createdByUserId,
        IReadOnlyList<(Guid stockItemId, string materialCode, decimal quantity)> items,
        TimeSpan ttl)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (correlationId == Guid.Empty) throw new ArgumentException("CorrelationId required.", nameof(correlationId));
        if (string.IsNullOrWhiteSpace(consumerModule)) throw new ArgumentException("ConsumerModule required.", nameof(consumerModule));

        // I-01: at least one item
        if (items == null || items.Count == 0)
            throw new ArgumentException("At least one reservation item is required (I-01).", nameof(items));

        // I-03: TTL bounds
        if (ttl < MinTtl)
            throw new ArgumentException($"TTL must be at least {MinTtl.TotalHours}h (I-03).", nameof(ttl));
        if (ttl > MaxTtl)
            throw new ArgumentException($"TTL must not exceed {MaxTtl.TotalHours}h (I-03).", nameof(ttl));

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now + ttl;

        // I-02: ExpiresAt > CreatedAt (guaranteed by positive TTL, but guard explicitly)
        if (expiresAt <= now)
            throw new ArgumentException("ExpiresAt must be after CreatedAt (I-02).", nameof(ttl));

        var reservationId = Guid.NewGuid();
        var reservation = new Reservation
        {
            Id = reservationId,
            TenantId = tenantId,
            CorrelationId = correlationId,
            ConsumerModule = consumerModule,
            ConsumerContextJson = contextJson,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Status = ReservationStatus.Active
        };

        foreach (var (stockItemId, materialCode, quantity) in items)
        {
            // I-06: positive quantity; I-10: item TenantId == Reservation.TenantId
            var item = ReservationItem.Create(reservationId, tenantId, stockItemId, materialCode, quantity);
            reservation._items.Add(item);
        }

        reservation.RaiseDomainEvent(new StockReservedDomainEvent(
            reservation.Id,
            reservation.TenantId,
            reservation.CorrelationId,
            reservation.ConsumerModule,
            reservation.ExpiresAt));

        return reservation;
    }

    /// <summary>
    /// Releases an Active reservation, freeing the reserved stock. Enforces I-05.
    /// Raises <see cref="ReservationReleasedDomainEvent"/>.
    /// </summary>
    /// <param name="reason">Optional free-text reason for audit.</param>
    public void Release(string? reason)
    {
        EnsureActive("release");
        Status = ReservationStatus.Released;
        RaiseDomainEvent(new ReservationReleasedDomainEvent(Id, TenantId, reason));
    }

    /// <summary>
    /// Marks the reservation as Expired. Must only be called by the expiry worker (I-08).
    /// Enforces I-05 and I-08.
    /// Raises <see cref="ReservationExpiredDomainEvent"/>.
    /// </summary>
    /// <param name="isWorkerContext">Must be <c>true</c>; guards against accidental invocation (I-08).</param>
    public void MarkExpired(bool isWorkerContext = false)
    {
        // I-08: only worker may expire
        if (!isWorkerContext)
            throw new InvalidOperationException("MarkExpired can only be invoked by the expiry worker (I-08).");
        EnsureActive("expire");
        Status = ReservationStatus.Expired;
        RaiseDomainEvent(new ReservationExpiredDomainEvent(Id, TenantId));
    }

    /// <summary>
    /// Marks the reservation as Consumed once all stock has been used. Enforces I-05.
    /// Raises <see cref="ReservationConsumedDomainEvent"/>.
    /// </summary>
    public void MarkConsumed()
    {
        EnsureActive("consume");
        Status = ReservationStatus.Consumed;
        RaiseDomainEvent(new ReservationConsumedDomainEvent(Id, TenantId));
    }

    // I-05: terminal states are irreversible
    private void EnsureActive(string operation)
    {
        if (Status != ReservationStatus.Active)
            throw new InvalidOperationException(
                $"Cannot {operation} a reservation in '{Status}' state. Only Active reservations support this transition (I-05).");
    }
}
