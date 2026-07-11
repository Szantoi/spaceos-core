// SpaceOS.Infrastructure/Outbox/NullSignalROutboxFanOut.cs

using SpaceOS.Kernel.Domain.Outbox;

namespace SpaceOS.Infrastructure.Outbox;

/// <summary>
/// No-op implementation of <see cref="ISignalROutboxFanOut"/> used until Cutting Phase 4 Track B
/// provides the real SignalR fan-out.
/// </summary>
internal sealed class NullSignalROutboxFanOut : ISignalROutboxFanOut
{
    /// <inheritdoc/>
    public Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
        => Task.CompletedTask;
}
