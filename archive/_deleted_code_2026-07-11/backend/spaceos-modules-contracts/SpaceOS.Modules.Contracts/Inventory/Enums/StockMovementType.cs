namespace SpaceOS.Modules.Contracts.Inventory.Enums;

/// <summary>Classifies the reason for a stock quantity change.</summary>
public enum StockMovementType
{
    /// <summary>Stock arrived from a supplier or purchase order.</summary>
    Inbound,

    /// <summary>Stock was consumed during production.</summary>
    Consumed,

    /// <summary>An offcut was created from a cutting operation and added to stock.</summary>
    OffcutCreated,

    /// <summary>Stock was scrapped due to damage or quality failure.</summary>
    Scrapped,

    /// <summary>Stock quantity was manually corrected (e.g. after stock count).</summary>
    Adjusted,

    /// <summary>Stock was returned from production back to the warehouse.</summary>
    Returned,
}
