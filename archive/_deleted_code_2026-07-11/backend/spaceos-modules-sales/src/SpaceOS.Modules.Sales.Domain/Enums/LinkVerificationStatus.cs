namespace SpaceOS.Modules.Sales.Domain.Enums;

/// <summary>
/// SEC-S-02: Tracks the B2B handshake verification state for a Customer ↔ PlatformActor link.
/// </summary>
public enum LinkVerificationStatus
{
    None = 0,
    Pending = 1,
    Verified = 2
}
