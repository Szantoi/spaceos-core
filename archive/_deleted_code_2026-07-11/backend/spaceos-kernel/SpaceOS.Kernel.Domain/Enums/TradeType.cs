namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Identifies the construction or engineering trade that a
/// <see cref="SpaceOS.Kernel.Domain.Entities.SpaceLayer"/> or
/// <see cref="SpaceOS.Kernel.Domain.Entities.SpatialElement"/> represents.
/// </summary>
public enum TradeType
{
    /// <summary>Joinery trade (legacy).</summary>
    Joinery      = 1,

    /// <summary>Plumbing trade (legacy).</summary>
    Plumbing     = 2,

    /// <summary>Electrical trade (legacy).</summary>
    Electrical   = 3,

    /// <summary>Architecture trade (legacy).</summary>
    Architecture = 4,

    /// <summary>Mechanical, electrical and plumbing trade (legacy).</summary>
    Mep          = 5,

    /// <summary>Door element.</summary>
    Door         = 10,

    /// <summary>Window element.</summary>
    Window       = 11,

    /// <summary>Cabinet or carcass element.</summary>
    Cabinet      = 12,

    /// <summary>Wall element.</summary>
    Wall         = 13,

    /// <summary>Opening element.</summary>
    Opening      = 14,

    /// <summary>Shelf element.</summary>
    Shelf        = 15
}
