namespace SpaceOS.Modules.Inventory.Domain.Services;

/// <summary>
/// V1 implementation of <see cref="IModuleRegistry"/> backed by a hardcoded allowlist.
/// Replace with a database-driven implementation when the module catalogue grows.
/// </summary>
public sealed class HardcodedModuleRegistry : IModuleRegistry
{
    private static readonly HashSet<string> _allowedModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cutting",
        "Joinery",
        "Cabinet",
        "FreeTier"
    };

    /// <inheritdoc />
    public bool IsKnownConsumerModule(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName)) return false;
        return _allowedModules.Contains(moduleName);
    }
}
