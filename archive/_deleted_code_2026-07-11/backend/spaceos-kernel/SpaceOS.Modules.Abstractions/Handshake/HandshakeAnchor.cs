// SpaceOS.Modules.Abstractions/Handshake/HandshakeAnchor.cs
namespace SpaceOS.Modules.Abstractions.Handshake;

/// <summary>
/// Immutable value object that anchors one side of a handshake to a specific
/// SpaceOS entity owned by a tenant.
/// </summary>
/// <param name="Type">The hierarchy level of the anchor.</param>
/// <param name="TenantId">The tenant that owns the anchored entity.</param>
/// <param name="FlowNodeId">The identifier of the anchored flow node.</param>
public sealed record HandshakeAnchor(AnchorType Type, Guid TenantId, Guid FlowNodeId);
