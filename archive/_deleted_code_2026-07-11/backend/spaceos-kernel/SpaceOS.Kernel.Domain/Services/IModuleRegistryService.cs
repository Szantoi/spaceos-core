// SpaceOS.Kernel.Domain/Services/IModuleRegistryService.cs
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Domain service that encodes the static module-permission rules per <see cref="TenantType"/>.
/// <para>
/// The registry is intentionally static (hard-coded) rather than DB-sourced so that the rules
/// are available at domain validation time without any infrastructure dependency (SEC-02
/// defence-in-depth: Application-layer validation mirrors the DB trigger).
/// </para>
/// </summary>
public interface IModuleRegistryService
{
    /// <summary>
    /// Validates that every module in <paramref name="enabledModules"/> is allowed for the
    /// given <paramref name="tenantType"/>, and that all required modules are present.
    /// </summary>
    /// <param name="tenantType">The tenant type whose rules apply.</param>
    /// <param name="enabledModules">The list of module names to validate (lowercase identifiers).</param>
    /// <returns>
    /// A <see cref="ModuleValidationResult"/> that is successful when all constraints are
    /// satisfied, or carries an error message when validation fails.
    /// </returns>
    ModuleValidationResult ValidateModulesForTenantType(TenantType tenantType, IReadOnlyList<string> enabledModules);

    /// <summary>Returns the modules that are mandatory for <paramref name="tenantType"/>.</summary>
    /// <param name="tenantType">The tenant type to query.</param>
    /// <returns>An ordered, non-null list of required module name strings.</returns>
    IReadOnlyList<string> GetRequiredModules(TenantType tenantType);

    /// <summary>
    /// Returns all modules that are permitted for <paramref name="tenantType"/> (required + optional).
    /// </summary>
    /// <param name="tenantType">The tenant type to query.</param>
    /// <returns>An ordered, non-null list of allowed module name strings.</returns>
    IReadOnlyList<string> GetAllowedModules(TenantType tenantType);
}
