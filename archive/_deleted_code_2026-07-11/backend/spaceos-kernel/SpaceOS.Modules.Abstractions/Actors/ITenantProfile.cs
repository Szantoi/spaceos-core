// SpaceOS.Modules.Abstractions/Actors/ITenantProfile.cs
namespace SpaceOS.Modules.Abstractions.Actors;

/// <summary>
/// Read-only profile of a tenant node as exposed at the module boundary.
/// </summary>
public interface ITenantProfile
{
    /// <summary>Gets the unique identifier of this tenant.</summary>
    Guid TenantId { get; }

    /// <summary>Gets the display name of this tenant.</summary>
    string Name { get; }

    /// <summary>Gets the supply-chain role of this tenant.</summary>
    TenantType Type { get; }
}
