namespace SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Composite capability flags for module providers.
/// Consumer MUST check HasFlag before calling optional methods (SEC-05).
/// If provider claims capability but doesn't implement it, throw NotSupportedException.
/// </summary>
[Flags]
public enum ProviderCapability
{
    /// <summary>No capabilities declared.</summary>
    None               = 0,

    /// <summary>Provider supports submitting cutting sheets.</summary>
    CuttingSubmit      = 1 << 0,

    /// <summary>Provider supports nesting optimization.</summary>
    CuttingNesting     = 1 << 1,

    /// <summary>Provider supports execution tracking.</summary>
    CuttingExecution   = 1 << 2,

    /// <summary>Provider supports waste reporting.</summary>
    CuttingWaste       = 1 << 3,

    /// <summary>Provider supports stock queries.</summary>
    InventoryStock     = 1 << 4,

    /// <summary>Provider supports offcut management.</summary>
    InventoryOffcut    = 1 << 5,

    /// <summary>Provider supports consumption trend analysis.</summary>
    InventoryTrend     = 1 << 6,

    /// <summary>Provider supports stock location queries.</summary>
    InventoryLocation  = 1 << 7,

    /// <summary>Provider supports purchase order creation.</summary>
    ProcurementOrder   = 1 << 8,

    /// <summary>Provider supports supplier price list queries.</summary>
    ProcurementPricing = 1 << 9,

    /// <summary>Provider supports supplier rating queries.</summary>
    ProcurementRating  = 1 << 10,

    /// <summary>Provider supports soft stock reservation with TTL (v1.2.0).</summary>
    InventoryReservation = 1 << 11,

    /// <summary>
    /// Provider supports anonymous/unauthenticated cutting sheet submission (v1.3.0).
    /// Required for FreeTier and PartnerTier flows.
    /// Consumer MUST check this flag before calling SubmitAnonymousSheetAsync (SEC-05).
    /// </summary>
    CuttingAnonymous = 1 << 12,
}
