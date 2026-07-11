using System.Collections.Concurrent;

namespace SpaceOS.Modules.Inventory.Infrastructure.Services;

/// <summary>
/// In-memory implementation of <see cref="IWorkerHeartbeatStore"/>.
/// Sufficient for single-instance v1 deployment; replace with a Redis-backed implementation
/// when horizontal scaling is required.
/// </summary>
public sealed class InMemoryWorkerHeartbeatStore : IWorkerHeartbeatStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _ticks = new();

    /// <inheritdoc/>
    public Task TickAsync(string workerName, CancellationToken ct)
    {
        _ticks[workerName] = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<DateTimeOffset?> GetLastTickAsync(string workerName, CancellationToken ct)
    {
        var result = _ticks.TryGetValue(workerName, out var t)
            ? (DateTimeOffset?)t
            : null;
        return Task.FromResult(result);
    }
}
