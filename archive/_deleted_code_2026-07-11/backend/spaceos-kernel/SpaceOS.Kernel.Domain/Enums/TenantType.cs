// SpaceOS.Kernel.Domain/Enums/TenantType.cs
namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Classifies a <see cref="SpaceOS.Kernel.Domain.Entities.Tenant"/> by its role in the
/// SpaceOS ecosystem supply chain.
/// <para>
/// The value is immutable after tenant creation (enforced at both the application and
/// database layers). Changing TenantType would invalidate the ModuleRegistry
/// validation rules and the B2B handshake graph.
/// </para>
/// </summary>
public enum TenantType
{
    /// <summary>Manufactures finished products (doors, cabinets, windows). Default type.</summary>
    Manufacturer = 0,

    /// <summary>Specialises in panel cutting (szabászat). Core module: cutting.</summary>
    PanelCutter = 1,

    /// <summary>Materials/product trader. Core module: trading.</summary>
    Trader = 2,

    /// <summary>Logistics and delivery provider. Core module: delivery.</summary>
    Logistics = 3,

    /// <summary>On-site installer. Core module: installation.</summary>
    Installer = 4,

    /// <summary>End customer (buyer). Core module: orders.</summary>
    EndCustomer = 5,
}
