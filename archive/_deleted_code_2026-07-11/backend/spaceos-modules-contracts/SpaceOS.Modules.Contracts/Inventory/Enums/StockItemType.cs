namespace SpaceOS.Modules.Contracts.Inventory.Enums;

/// <summary>Distinguishes whether a stock item is a full-size panel or a reusable offcut.</summary>
public enum StockItemType
{
    /// <summary>A full, uncut panel as received from the supplier.</summary>
    FullPanel,

    /// <summary>A remaining piece produced as a by-product of a previous cutting operation.</summary>
    Offcut,
}
