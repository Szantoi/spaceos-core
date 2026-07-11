// SpaceOS.Modules.Abstractions/Handshake/IHandshake.cs
namespace SpaceOS.Modules.Abstractions.Handshake;

/// <summary>
/// Cross-module contract for a handshake — a bilateral commitment between two
/// tenant nodes within a SpaceOS workflow.
/// </summary>
public interface IHandshake
{
    /// <summary>Gets the unique identifier of this handshake.</summary>
    Guid Id { get; }

    /// <summary>Gets the tenant identifier of the party who initiated the handshake.</summary>
    Guid InitiatorTenantId { get; }

    /// <summary>Gets the tenant identifier of the party responsible for delivery.</summary>
    Guid ResponsibleTenantId { get; }

    /// <summary>
    /// Gets the optional anchor that links the initiator side of this handshake to a
    /// specific SpaceOS entity.
    /// </summary>
    HandshakeAnchor? InitiatorAnchor { get; }

    /// <summary>
    /// Gets the optional anchor that links the responsible side of this handshake to a
    /// specific SpaceOS entity.
    /// </summary>
    HandshakeAnchor? ResponsibleAnchor { get; }

    /// <summary>Gets the data visibility level agreed between both parties.</summary>
    HandshakeVisibilityScope Scope { get; }

    /// <summary>Gets the current FSM state of this handshake.</summary>
    HandshakeState State { get; }

    /// <summary>Gets the directional type of this handshake.</summary>
    HandshakeType Type { get; }

    /// <summary>
    /// Gets the optional SHA-256 hash of the contract document attached to this handshake.
    /// <c>null</c> when no contract has been attached.
    /// </summary>
    string? ContractHash { get; }

    /// <summary>Gets the UTC timestamp at which this handshake was created.</summary>
    DateTimeOffset CreatedAt { get; }
}
