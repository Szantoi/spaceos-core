namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Identifies the manufacturing or installation phase that a
/// <see cref="SpaceOS.Kernel.Domain.Entities.SpatialTaskLink"/> tracks.
/// </summary>
public enum WorkPhase
{
    /// <summary>Dimensional measurement and verification.</summary>
    Measurement = 1,

    /// <summary>Material cutting operations.</summary>
    Cutting = 2,

    /// <summary>Edge banding or finishing of cut edges.</summary>
    Edging = 3,

    /// <summary>Component assembly.</summary>
    Assembly = 4,

    /// <summary>Surface finishing (paint, lacquer, veneer).</summary>
    Finishing = 5,

    /// <summary>On-site installation.</summary>
    Installation = 6
}
