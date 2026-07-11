namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Classifies the type of a <see cref="SpaceOS.Kernel.Domain.Aggregates.PhysicalSpace"/>.
/// </summary>
public enum SpaceType
{
    /// <summary>An enclosed room within a facility.</summary>
    Room = 1,

    /// <summary>A corridor or hallway connecting spaces.</summary>
    Corridor = 2,

    /// <summary>An exterior or outdoor area.</summary>
    Exterior = 3,

    /// <summary>A vertical shaft (elevator, ventilation, plumbing).</summary>
    Shaft = 4
}
