namespace SpaceOS.Modules.Inventory.Domain.Services;

/// <summary>
/// Determines whether a module name is a known, allowed consumer of the Inventory reservation API.
/// Validates invariant I-12.
/// </summary>
public interface IModuleRegistry
{
    /// <summary>Returns <c>true</c> if <paramref name="moduleName"/> is in the known allowlist.</summary>
    bool IsKnownConsumerModule(string moduleName);
}
