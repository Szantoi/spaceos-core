// SpaceOS.Modules.Abstractions/Handshake/AnchorType.cs
namespace SpaceOS.Modules.Abstractions.Handshake;

/// <summary>
/// Identifies the kind of SpaceOS entity that a <see cref="HandshakeAnchor"/> points to.
/// </summary>
public enum AnchorType
{
    /// <summary>The anchor targets a FlowEpic.</summary>
    Epic = 1,

    /// <summary>The anchor targets a project-level entity.</summary>
    Project = 2,

    /// <summary>The anchor targets a program-level entity.</summary>
    Program = 3,
}
