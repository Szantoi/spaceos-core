using System.Diagnostics.Metrics;

namespace SpaceOS.Modules.Inventory.Infrastructure.Observability;

/// <summary>
/// OpenTelemetry metrics instruments for the Inventory Reservation subsystem.
/// All counters use the unit <c>{reservation}</c> per OTel semantic conventions.
/// </summary>
public static class ReservationMetrics
{
    private static readonly Meter Meter = new("SpaceOS.Inventory.Reservations", "1.0.0");

    /// <summary>Number of reservations successfully created.</summary>
    public static readonly Counter<long> ReservationsCreated =
        Meter.CreateCounter<long>("reservations.created.total", "{reservation}");

    /// <summary>Number of reservations explicitly released by consumers.</summary>
    public static readonly Counter<long> ReservationsReleased =
        Meter.CreateCounter<long>("reservations.released.total", "{reservation}");

    /// <summary>Number of reservations expired by the cleanup worker.</summary>
    public static readonly Counter<long> ReservationsExpired =
        Meter.CreateCounter<long>("reservations.expired.total", "{reservation}");

    /// <summary>Number of reservations marked as consumed.</summary>
    public static readonly Counter<long> ReservationsConsumed =
        Meter.CreateCounter<long>("reservations.consumed.total", "{reservation}");

    /// <summary>Duration of a single reserve/release/get request in milliseconds.</summary>
    public static readonly Histogram<double> ReservationDurationMs =
        Meter.CreateHistogram<double>("reservations.request.duration", "ms");

    /// <summary>Duration of a single cleanup worker iteration in milliseconds.</summary>
    public static readonly Histogram<double> CleanupIterationMs =
        Meter.CreateHistogram<double>("reservations.cleanup.iteration.duration", "ms");

    /// <summary>Number of times an idempotent reserve request returned an existing reservation.</summary>
    public static readonly Counter<long> IdempotencyHits =
        Meter.CreateCounter<long>("reservations.idempotency.hit.total", "{reservation}");
}
