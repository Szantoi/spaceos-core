// SpaceOS.Kernel.Domain/Outbox/OutboxStatus.cs

namespace SpaceOS.Kernel.Domain.Outbox;

/// <summary>
/// Processing status of an <see cref="OutboxMessage"/>.
/// </summary>
public enum OutboxStatus
{
    /// <summary>Message was successfully dispatched.</summary>
    Processed = 0,

    /// <summary>Message is waiting to be dispatched.</summary>
    Pending = 1,

    /// <summary>Dispatch failed; message will not be retried automatically.</summary>
    Failed = 2
}
