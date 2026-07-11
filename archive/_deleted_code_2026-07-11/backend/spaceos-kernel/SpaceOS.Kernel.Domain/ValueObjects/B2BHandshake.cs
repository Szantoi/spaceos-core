namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Value object that records the details of a B2B delegation between the owning tenant
/// and a guest tenant for a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/>.
/// </summary>
public sealed record B2BHandshake
{
    /// <summary>Gets the identifier of the guest tenant that accepted the delegation.</summary>
    public TenantId GuestTenantId { get; init; }

    /// <summary>Gets the UTC timestamp at which the delegation occurred.</summary>
    public DateTimeOffset DelegatedOn { get; init; }

    // Sprint C extensions — all nullable for backward compatibility.
    // Anchors are stored as raw JSON to avoid a Domain dependency on SpaceOS.Modules.Abstractions.
    // Serialization/deserialization happens at the Application/Infrastructure layer.

    /// <summary>
    /// Gets the JSON-serialized <c>HandshakeAnchor</c> for the initiating party,
    /// or <see langword="null"/> when not set.
    /// </summary>
    public string? InitiatorAnchorJson { get; init; }

    /// <summary>
    /// Gets the JSON-serialized <c>HandshakeAnchor</c> for the responsible party,
    /// or <see langword="null"/> when not set.
    /// </summary>
    public string? ResponsibleAnchorJson { get; init; }

    /// <summary>
    /// Gets the <c>HandshakeVisibilityScope</c> enum value as a string,
    /// or <see langword="null"/> when not set.
    /// </summary>
    public string? VisibilityScope { get; init; }

    /// <summary>
    /// Gets the SHA-256 contract hash that both parties have agreed on,
    /// or <see langword="null"/> when not set.
    /// </summary>
    public string? ContractHash { get; init; }

    /// <summary>
    /// Initialises a new <see cref="B2BHandshake"/> with the given guest tenant and timestamp.
    /// Sprint C properties are init-only and default to <see langword="null"/>.
    /// </summary>
    /// <param name="guestTenantId">The identifier of the guest tenant.</param>
    /// <param name="delegatedOn">The UTC timestamp of the delegation.</param>
    public B2BHandshake(TenantId guestTenantId, DateTimeOffset delegatedOn)
    {
        GuestTenantId = guestTenantId;
        DelegatedOn = delegatedOn;
    }
}
