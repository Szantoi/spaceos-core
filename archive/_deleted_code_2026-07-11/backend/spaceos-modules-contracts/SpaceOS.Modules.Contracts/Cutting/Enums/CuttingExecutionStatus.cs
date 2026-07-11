namespace SpaceOS.Modules.Contracts.Cutting.Enums;

/// <summary>Represents the execution lifecycle state of a cutting sheet on the shop floor.</summary>
public enum CuttingExecutionStatus
{
    /// <summary>The sheet has been scheduled but execution has not started.</summary>
    Planned,

    /// <summary>Cutting is currently in progress.</summary>
    InProgress,

    /// <summary>Cutting completed successfully.</summary>
    Completed,

    /// <summary>Cutting failed; see associated CuttingFailed event for the reason.</summary>
    Failed,
}
