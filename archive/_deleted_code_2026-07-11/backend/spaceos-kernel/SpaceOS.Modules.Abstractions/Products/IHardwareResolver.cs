// SpaceOS.Modules.Abstractions/Products/IHardwareResolver.cs
namespace SpaceOS.Modules.Abstractions.Products;

/// <summary>
/// Resolves the hardware bill-of-materials for a parametric product given a
/// JSON-encoded set of configuration parameters.
/// </summary>
public interface IHardwareResolver
{
    /// <summary>
    /// Returns the list of hardware components required to build a product with
    /// the supplied <paramref name="parametersJson"/> configuration.
    /// </summary>
    /// <param name="parametersJson">
    /// A JSON object whose keys are parameter names and whose values are the
    /// configured values (e.g. <c>{"HingeCount":4,"DrawerCount":2}</c>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A JSON-encoded hardware list document; never <c>null</c> or empty.
    /// </returns>
    Task<string> ResolveAsync(string parametersJson, CancellationToken ct = default);
}
