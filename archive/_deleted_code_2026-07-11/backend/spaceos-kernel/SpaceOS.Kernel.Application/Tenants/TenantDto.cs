namespace SpaceOS.Kernel.Application.Tenants;

/// <summary>Data transfer object representing a tenant returned by application queries.</summary>
/// <param name="Id">The unique identifier of the tenant.</param>
/// <param name="Name">The display name of the tenant.</param>
/// <param name="TenantType">The ecosystem actor type (e.g. "Manufacturer", "Trader").</param>
/// <param name="EnabledModules">The list of enabled module names for this tenant.</param>
public record TenantDto(Guid Id, string Name, string TenantType, IReadOnlyList<string> EnabledModules);
