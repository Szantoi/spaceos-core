// SpaceOS.Kernel.Domain/Enums/ModuleType.cs
namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Enumerates all known SpaceOS functional modules that can be enabled for a tenant.
/// <para>
/// The string representations (lowercase) are stored in the <c>EnabledModules</c>
/// PostgreSQL array column. The numeric values group modules by supply-chain domain:
/// 0–2 manufacturing, 10–11 spatial/production, 20–23 trade/logistics.
/// </para>
/// </summary>
public enum ModuleType
{
    /// <summary>Door manufacturing module.</summary>
    Door = 0,

    /// <summary>Cabinet/furniture manufacturing module.</summary>
    Cabinet = 1,

    /// <summary>Window manufacturing module.</summary>
    Window = 2,

    /// <summary>Panel cutting (szabászat) module.</summary>
    Cutting = 10,

    /// <summary>3-D spatial design module.</summary>
    Spatial = 11,

    /// <summary>Materials/product trading module.</summary>
    Trading = 20,

    /// <summary>Delivery/logistics module.</summary>
    Delivery = 21,

    /// <summary>On-site installation module.</summary>
    Installation = 22,

    /// <summary>Customer orders module.</summary>
    Orders = 23,
}
