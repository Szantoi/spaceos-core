// SpaceOS.Kernel.Application/DTOs/TenantClaimDto.cs
using System.Text.Json.Serialization;

namespace SpaceOS.Kernel.Application.DTOs;

/// <summary>
/// Represents a single tenant entry from the Keycloak <c>spaceos_tenants</c> JWT claim.
/// Keycloak's Script Mapper serialises the tenant list as a JSON array embedded in the token.
/// </summary>
public sealed record TenantClaimDto
{
    /// <summary>The tenant's UUID.</summary>
    [JsonPropertyName("tenant_id")]
    public required string TenantId { get; init; }

    /// <summary>The tenant's type (e.g. Producer, Retailer).</summary>
    [JsonPropertyName("tenant_type")]
    public required string TenantType { get; init; }

    /// <summary>The list of enabled module identifiers for this tenant.</summary>
    [JsonPropertyName("enabled_modules")]
    public required string[] EnabledModules { get; init; }

    /// <summary>The brand skin identifier for this tenant's UI theme.</summary>
    [JsonPropertyName("brand_skin")]
    public required string BrandSkin { get; init; }
}
