// SpaceOS.Modules.Abstractions/Handshake/HandshakeState.cs
namespace SpaceOS.Modules.Abstractions.Handshake;

/// <summary>
/// FSM states for the lifecycle of a handshake between two SpaceOS nodes.
/// </summary>
public enum HandshakeState
{
    /// <summary>The initiator has proposed the handshake; awaiting response.</summary>
    Proposed = 1,

    /// <summary>The responsible party has accepted the proposal.</summary>
    Accepted = 2,

    /// <summary>Both parties are actively engaged on delivery.</summary>
    InProgress = 3,

    /// <summary>Delivery is complete and the initiator's approval is pending.</summary>
    AwaitingApproval = 4,

    /// <summary>All obligations have been fulfilled and the initiator has approved.</summary>
    Completed = 5,

    /// <summary>The responsible party has declined the proposal.</summary>
    Rejected = 6,

    /// <summary>The initiator has withdrawn the handshake.</summary>
    Cancelled = 7,
}
