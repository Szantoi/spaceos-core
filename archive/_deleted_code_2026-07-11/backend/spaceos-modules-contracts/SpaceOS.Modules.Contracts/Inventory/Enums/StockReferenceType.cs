namespace SpaceOS.Modules.Contracts.Inventory.Enums;

/// <summary>Identifies the type of external document that caused a stock movement.</summary>
public enum StockReferenceType
{
    /// <summary>Movement originated from a cutting sheet execution.</summary>
    CuttingSheet,

    /// <summary>Movement originated from a purchase order delivery.</summary>
    PurchaseOrder,

    /// <summary>Movement was recorded manually without an external reference.</summary>
    Manual,

    /// <summary>Movement resulted from a physical stock count correction.</summary>
    StockCount,

    /// <summary>Movement is a return from production to the warehouse.</summary>
    Return,
}
