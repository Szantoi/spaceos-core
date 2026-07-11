// SpaceOS.Modules.Abstractions/Handshake/HandshakeType.cs
namespace SpaceOS.Modules.Abstractions.Handshake;

/// <summary>
/// Identifies the directional relationship between the two parties in a handshake,
/// representing the role-to-role flow in the SpaceOS supply chain.
/// </summary>
public enum HandshakeType
{
    /// <summary>Handshake initiated by a designer toward a manufacturer.</summary>
    DesignToManufacturer = 1,

    /// <summary>Handshake initiated by a manufacturer toward a supplier.</summary>
    ManufacturerToSupplier = 2,

    /// <summary>Handshake initiated by a designer toward an installer.</summary>
    DesignToInstaller = 3,

    /// <summary>Handshake initiated by a client toward a designer.</summary>
    ClientToDesigner = 4,

    /// <summary>Handshake initiated by a dealer toward a manufacturer.</summary>
    DealerToManufacturer = 5,

    /// <summary>Handshake initiated by a supplier toward a manufacturer.</summary>
    SupplierToManufacturer = 6,

    /// <summary>Handshake initiated by an installer toward a supplier.</summary>
    InstallerToSupplier = 7,

    /// <summary>A self-assigned task with a single actor on both sides.</summary>
    SelfTask = 8,
}
