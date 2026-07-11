// SpaceOS.Modules.Abstractions/Products/IParametricProduct.cs
namespace SpaceOS.Modules.Abstractions.Products;

/// <summary>
/// Describes a configurable product whose dimensions and bill-of-materials are
/// derived from a set of named parameters at order time.
/// </summary>
public interface IParametricProduct
{
    /// <summary>Gets the unique identifier of this product definition.</summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the stock-keeping unit code that uniquely identifies this product
    /// within the manufacturer's catalogue.
    /// </summary>
    string Sku { get; }

    /// <summary>Gets the human-readable product name.</summary>
    string Name { get; }

    /// <summary>
    /// Gets the JSON-encoded parameter schema for this product.
    /// The schema defines the names, types, and constraints of configuration
    /// values accepted by the cut-list and hardware calculators.
    /// </summary>
    string ParametersJson { get; }
}
