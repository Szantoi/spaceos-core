namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Represents the operational status of a <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/>.
/// </summary>
public enum WorkStationStatus
{
    /// <summary>The workstation is free and ready to be used.</summary>
    Available = 0,

    /// <summary>The workstation is currently in use.</summary>
    Occupied = 1,

    /// <summary>The workstation is undergoing maintenance and is not available.</summary>
    Maintenance = 2,

    /// <summary>The workstation is no longer supported or current.</summary>
    Outdated = 3,

    /// <summary>Reserved for future use: a workstation actively executing a FlowEpic.</summary>
    Active = 4
}
