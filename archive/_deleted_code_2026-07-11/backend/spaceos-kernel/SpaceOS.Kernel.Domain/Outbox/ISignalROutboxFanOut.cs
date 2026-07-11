// SpaceOS.Kernel.Domain/Outbox/ISignalROutboxFanOut.cs

namespace SpaceOS.Kernel.Domain.Outbox;

/// <summary>
/// Abstraction for fanning out outbox messages to SignalR real-time clients.
/// The concrete implementation is provided by Cutting Phase 4 Track B.
/// </summary>
public interface ISignalROutboxFanOut
{
    /// <summary>
    /// Dispatches an outbox message to all relevant SignalR hubs.
    /// </summary>
    /// <param name="message">The message to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DispatchAsync(OutboxMessage message, CancellationToken ct = default);
}
