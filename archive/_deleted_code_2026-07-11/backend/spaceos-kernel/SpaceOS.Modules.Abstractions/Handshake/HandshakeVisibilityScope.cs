// SpaceOS.Modules.Abstractions/Handshake/HandshakeVisibilityScope.cs
namespace SpaceOS.Modules.Abstractions.Handshake;

/// <summary>
/// Controls how much information each party in a handshake can see.
/// </summary>
public enum HandshakeVisibilityScope
{
    /// <summary>Only public milestone data is visible to both parties.</summary>
    PublicMilestone = 1,

    /// <summary>Delivery schedule and status are shared.</summary>
    DeliveryOnly = 2,

    /// <summary>Specification documents are also shared.</summary>
    SpecShared = 3,

    /// <summary>Full transparency — all data is visible to both parties.</summary>
    FullTransparent = 4,
}
