// SpaceOS.Modules.Abstractions/Actors/ManufacturerType.cs
namespace SpaceOS.Modules.Abstractions.Actors;

/// <summary>
/// Identifies the category of goods a manufacturer specialises in.
/// Drives which cut-list and pricing engines are applied to their products.
/// </summary>
public enum ManufacturerType
{
    /// <summary>Produces cabinet carcasses and storage units.</summary>
    Cabinet = 1,

    /// <summary>Produces interior and exterior doors.</summary>
    Door = 2,

    /// <summary>Produces windows and glazed units.</summary>
    Window = 3,
}
