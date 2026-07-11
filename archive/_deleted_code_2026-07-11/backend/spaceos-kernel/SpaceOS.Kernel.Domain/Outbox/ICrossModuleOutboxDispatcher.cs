// SpaceOS.Kernel.Domain/Outbox/ICrossModuleOutboxDispatcher.cs

namespace SpaceOS.Kernel.Domain.Outbox;

/// <summary>
/// Dispatches a processed <see cref="OutboxMessage"/> to all active cross-module subscribers
/// registered for the message's event type.
/// </summary>
public interface ICrossModuleOutboxDispatcher
{
    /// <summary>
    /// Dispatches the given <paramref name="message"/> to every active <c>ModuleSubscription</c>
    /// whose <c>EventType</c> matches the message's event type. If no subscribers are registered
    /// the call is a no-op.
    /// </summary>
    /// <param name="message">The outbox message to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
