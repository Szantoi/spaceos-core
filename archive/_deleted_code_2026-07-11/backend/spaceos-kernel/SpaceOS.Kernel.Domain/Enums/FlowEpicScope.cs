namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Represents the business scope of a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/>.
/// </summary>
public enum FlowEpicScope
{
    /// <summary>Epic tracks a door manufacturing order lifecycle.</summary>
    DoorOrder = 1,

    /// <summary>Epic tracks a cutting plan execution lifecycle.</summary>
    CuttingPlan = 2,

    /// <summary>Epic tracks a micro-assembly (cabinet component) lifecycle.</summary>
    MicroAssembly = 3
}
