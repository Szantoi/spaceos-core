// SpaceOS.Modules.Abstractions/Products/ICutListCalculator.cs
namespace SpaceOS.Modules.Abstractions.Products;

/// <summary>
/// Calculates the cut-list (panel schedule) for a parametric product given a
/// JSON-encoded set of configuration parameters.
/// Implementations are resolved per manufacturer type and registered in the
/// module's composition root.
/// </summary>
public interface ICutListCalculator
{
    /// <summary>
    /// Derives the ordered list of panels required to build a product with the
    /// supplied <paramref name="parametersJson"/> configuration.
    /// </summary>
    /// <param name="parametersJson">
    /// A JSON object whose keys are parameter names and whose values are the
    /// configured values (e.g. <c>{"Width":600,"Doors":2}</c>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A JSON-encoded cut-list document; never <c>null</c> or empty.
    /// </returns>
    Task<string> CalculateAsync(string parametersJson, CancellationToken ct = default);
}
