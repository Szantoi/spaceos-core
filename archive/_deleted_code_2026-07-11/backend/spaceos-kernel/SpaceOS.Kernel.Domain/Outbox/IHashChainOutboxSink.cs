// SpaceOS.Kernel.Domain/Outbox/IHashChainOutboxSink.cs

namespace SpaceOS.Kernel.Domain.Outbox;

/// <summary>
/// Abstraction for writing outbox message payloads into the audit hash chain.
/// The concrete implementation is provided by Cutting Phase 4 Track B.
/// </summary>
public interface IHashChainOutboxSink
{
    /// <summary>
    /// Appends the message payload to the audit hash chain.
    /// </summary>
    /// <param name="message">The message whose payload is to be recorded.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SinkAsync(OutboxMessage message, CancellationToken ct = default);
}
