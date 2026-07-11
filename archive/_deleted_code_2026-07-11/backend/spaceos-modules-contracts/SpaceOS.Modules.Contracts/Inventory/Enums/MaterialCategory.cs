namespace SpaceOS.Modules.Contracts.Inventory.Enums;

/// <summary>Top-level material classification used for reporting and purchasing rules.</summary>
public enum MaterialCategory
{
    /// <summary>Structural board panels (MDF, chipboard, plywood, solid wood).</summary>
    Board,

    /// <summary>Edge banding strips.</summary>
    Edge,

    /// <summary>Veneer sheets or rolls.</summary>
    Veneer,

    /// <summary>Hardware items (hinges, handles, drawer systems, etc.).</summary>
    Hardware,

    /// <summary>Adhesives and glues.</summary>
    Adhesive,

    /// <summary>Any material that does not fit the above categories.</summary>
    Other,
}
