// SpaceOS.Modules.Abstractions/Actors/TenantType.cs
namespace SpaceOS.Modules.Abstractions.Actors;

/// <summary>
/// Classifies the role a tenant plays within the SpaceOS supply chain.
/// </summary>
public enum TenantType
{
    /// <summary>Produces finished goods (cabinets, doors, windows, etc.).</summary>
    Manufacturer = 1,

    /// <summary>Supplies raw materials or components to manufacturers.</summary>
    Supplier = 2,

    /// <summary>Sells and distributes products to end clients.</summary>
    Dealer = 3,

    /// <summary>Performs on-site installation of products.</summary>
    Installer = 4,

    /// <summary>Creates design specifications and drawings.</summary>
    Designer = 5,

    /// <summary>End customer who commissions or purchases work.</summary>
    Client = 6,
}
