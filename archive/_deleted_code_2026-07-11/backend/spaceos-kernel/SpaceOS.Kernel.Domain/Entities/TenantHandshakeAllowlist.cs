// SpaceOS.Kernel.Domain/Entities/TenantHandshakeAllowlist.cs
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Represents a B2B handshake allowlist entry that permits a guest tenant
/// to initiate cross-tenant trade operations with a host tenant.
/// The composite key is (<see cref="GuestTenantId"/>, <see cref="HostTenantId"/>).
/// </summary>
public class TenantHandshakeAllowlist
{
    /// <summary>Gets the identifier of the guest (requesting) tenant.</summary>
    public Guid GuestTenantId { get; private set; }

    /// <summary>Gets the identifier of the host (providing) tenant.</summary>
    public Guid HostTenantId { get; private set; }

    private List<string> _allowedTradeTypes = new();

    /// <summary>Gets the trade types that the guest tenant is allowed to conduct with the host tenant.</summary>
    public IReadOnlyList<string> AllowedTradeTypes => _allowedTradeTypes.AsReadOnly();

    /// <summary>Gets the UTC timestamp when this allowlist entry was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Parameterless constructor reserved for EF Core materialisation.</summary>
    private TenantHandshakeAllowlist() { }

    /// <summary>
    /// Creates a new <see cref="TenantHandshakeAllowlist"/> entry after validating invariants.
    /// </summary>
    /// <param name="guestTenantId">The guest (requesting) tenant identifier. Must not equal <paramref name="hostTenantId"/>.</param>
    /// <param name="hostTenantId">The host (providing) tenant identifier.</param>
    /// <param name="allowedTradeTypes">
    /// The trade types to allow. Must contain at least one entry.
    /// Valid values: "door", "cabinet", "window".
    /// </param>
    /// <returns>A new <see cref="TenantHandshakeAllowlist"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when guest and host are the same, or when trade types are empty or invalid.</exception>
    public static TenantHandshakeAllowlist Create(
        Guid guestTenantId,
        Guid hostTenantId,
        IEnumerable<string> allowedTradeTypes)
    {
        if (guestTenantId == hostTenantId)
            throw new DomainException("Guest and host tenant must not be the same (no self-link).");

        var tradeTypes = allowedTradeTypes.ToList();
        if (tradeTypes.Count == 0)
            throw new DomainException("AllowedTradeTypes must contain at least one entry.");

        var valid = new[] { "door", "cabinet", "window" };
        if (tradeTypes.Any(t => !valid.Contains(t)))
            throw new DomainException($"Invalid trade type. Allowed: {string.Join(", ", valid)}");

        return new TenantHandshakeAllowlist
        {
            GuestTenantId = guestTenantId,
            HostTenantId = hostTenantId,
            _allowedTradeTypes = tradeTypes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
