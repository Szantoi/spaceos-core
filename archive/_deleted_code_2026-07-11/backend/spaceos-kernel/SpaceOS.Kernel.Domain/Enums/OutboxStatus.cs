// SpaceOS.Kernel.Domain/Enums/OutboxStatus.cs

namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Lifecycle status of an <c>OutboxEntry</c>.
/// Values are stored as strings in the database — do not rename existing members.
/// </summary>
public enum OutboxStatus
{
    /// <summary>The entry has not yet been picked up for processing.</summary>
    Pending = 0,

    /// <summary>The entry has been claimed by a worker and is being processed.</summary>
    Processing = 1,

    /// <summary>The entry was processed successfully.</summary>
    Processed = 2,

    /// <summary>
    /// The entry exceeded the maximum retry count and will no longer be retried.
    /// An <c>OutboxEntryDeadEvent</c> is raised when this status is reached.
    /// </summary>
    Dead = 3
}
