namespace SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Base interface for all module providers (cutting, inventory, procurement).
/// Every implementation MUST return within 5s on HealthCheckAsync — timeout = false.
/// Consumer MUST verify Capabilities before calling optional methods.
/// </summary>
public interface IModuleProvider
{
    /// <summary>Gets the unique name identifying this provider implementation.</summary>
    string ProviderName { get; }

    /// <summary>Gets the composite set of capabilities this provider supports.</summary>
    ProviderCapability Capabilities { get; }

    /// <summary>
    /// Checks whether the provider is healthy and ready to serve requests.
    /// Implementations MUST respond within 5 seconds.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the provider is healthy; otherwise <c>false</c>.</returns>
    Task<bool> HealthCheckAsync(CancellationToken ct);
}
