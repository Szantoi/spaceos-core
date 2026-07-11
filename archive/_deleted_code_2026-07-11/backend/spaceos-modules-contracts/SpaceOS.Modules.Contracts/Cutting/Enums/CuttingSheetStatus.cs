namespace SpaceOS.Modules.Contracts.Cutting.Enums;

/// <summary>Represents the workflow state of a cutting sheet from submission to completion.</summary>
public enum CuttingSheetStatus
{
    /// <summary>The sheet has been received and accepted.</summary>
    Received,

    /// <summary>The sheet is queued for nesting.</summary>
    Queued,

    /// <summary>Nesting optimization is running.</summary>
    InNesting,

    /// <summary>Nesting has been completed; the sheet is ready for execution.</summary>
    Nested,

    /// <summary>Physical cutting is in progress.</summary>
    InExecution,

    /// <summary>Cutting has been completed successfully.</summary>
    Completed,
}
