// SpaceOS.Kernel.Domain/Services/ModuleRegistryService.cs
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Static (hard-coded) implementation of <see cref="IModuleRegistryService"/>.
/// <para>
/// Rules are intentionally not DB-sourced. The static registry acts as
/// a defence-in-depth companion to the PostgreSQL <c>validate_enabled_modules_for_type()</c>
/// trigger (SEC-02): both layers must agree for a write to succeed.
/// </para>
/// </summary>
public sealed class ModuleRegistryService : IModuleRegistryService
{
    private static readonly IReadOnlyDictionary<TenantType, (string[] Required, string[] Optional)> Registry =
        new Dictionary<TenantType, (string[] Required, string[] Optional)>
        {
            [TenantType.Manufacturer] = (Array.Empty<string>(),    new[] { "door", "cabinet", "window", "cutting", "spatial" }),
            [TenantType.PanelCutter]  = (new[] { "cutting" },       Array.Empty<string>()),
            [TenantType.Trader]       = (new[] { "trading" },       new[] { "delivery" }),
            [TenantType.Logistics]    = (new[] { "delivery" },      Array.Empty<string>()),
            [TenantType.Installer]    = (new[] { "installation" },  Array.Empty<string>()),
            [TenantType.EndCustomer]  = (new[] { "orders" },        Array.Empty<string>()),
        };

    /// <inheritdoc/>
    public ModuleValidationResult ValidateModulesForTenantType(
        TenantType tenantType,
        IReadOnlyList<string> enabledModules)
    {
        if (!Registry.TryGetValue(tenantType, out var config))
            return ModuleValidationResult.Failure($"Unknown TenantType: {tenantType}");

        var allowed = new HashSet<string>(config.Required.Concat(config.Optional), StringComparer.Ordinal);

        var invalid = enabledModules.Where(m => !allowed.Contains(m)).ToList();
        if (invalid.Count > 0)
            return ModuleValidationResult.Failure(
                $"Modules not allowed for {tenantType}: {string.Join(", ", invalid)}");

        var missingRequired = config.Required.Where(r => !enabledModules.Contains(r)).ToList();
        if (missingRequired.Count > 0)
            return ModuleValidationResult.Failure(
                $"Required modules missing for {tenantType}: {string.Join(", ", missingRequired)}");

        return ModuleValidationResult.Success();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetRequiredModules(TenantType tenantType) =>
        Registry.TryGetValue(tenantType, out var c) ? c.Required : Array.Empty<string>();

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAllowedModules(TenantType tenantType) =>
        Registry.TryGetValue(tenantType, out var c)
            ? c.Required.Concat(c.Optional).ToArray()
            : Array.Empty<string>();
}
