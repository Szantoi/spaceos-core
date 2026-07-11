// SpaceOS.Infrastructure/Outbox/NullHashChainOutboxSink.cs

using SpaceOS.Kernel.Domain.Outbox;

namespace SpaceOS.Infrastructure.Outbox;

/// <summary>
/// No-op implementation of <see cref="IHashChainOutboxSink"/> used until Cutting Phase 4 Track B
/// provides the real hash-chain audit sink.
/// </summary>
internal sealed class NullHashChainOutboxSink : IHashChainOutboxSink
{
    /// <inheritdoc/>
    public Task SinkAsync(OutboxMessage message, CancellationToken ct = default)
        => Task.CompletedTask;
}
