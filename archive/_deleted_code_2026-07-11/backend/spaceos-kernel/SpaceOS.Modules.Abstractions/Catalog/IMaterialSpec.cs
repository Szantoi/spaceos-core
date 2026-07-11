// SpaceOS.Modules.Abstractions/Catalog/IMaterialSpec.cs
namespace SpaceOS.Modules.Abstractions.Catalog;

/// <summary>
/// Describes the physical properties of a board or sheet material available
/// from a supplier.
/// </summary>
public interface IMaterialSpec
{
    /// <summary>Gets the unique identifier of this material specification.</summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the substrate type of the material (e.g. <c>"MDF"</c>,
    /// <c>"Particle"</c>, <c>"Plywood"</c>).
    /// </summary>
    string MaterialType { get; }

    /// <summary>Gets the nominal thickness of the board in millimetres.</summary>
    decimal Thickness { get; }

    /// <summary>
    /// Gets the surface finish applied to the board (e.g. <c>"Melamine"</c>,
    /// <c>"Veneer"</c>, <c>"Laminate"</c>).
    /// </summary>
    string Surface { get; }
}
