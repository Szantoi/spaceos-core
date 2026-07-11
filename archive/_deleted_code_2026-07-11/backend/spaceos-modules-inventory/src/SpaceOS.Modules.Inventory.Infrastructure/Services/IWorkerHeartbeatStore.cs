namespace SpaceOS.Modules.Inventory.Infrastructure.Services;

/// <summary>
/// Records and retrieves heartbeat timestamps for background workers.
/// Used to verify that workers are alive without an external health-check system.
/// </summary>
public interface IWorkerHeartbeatStore
{
    /// <summary>Records a heartbeat tick for the given worker at the current UTC time.</summary>
    /// <param name="workerName">Logical name of the worker (e.g. "inventory-cleanup-worker").</param>
    /// <param name="ct">Cancellation token.</param>
    Task TickAsync(string workerName, CancellationToken ct);

    /// <summary>Returns the UTC timestamp of the last tick for <paramref name="workerName"/>,
    /// or <c>null</c> if no tick has been recorded yet.</summary>
    Task<DateTimeOffset?> GetLastTickAsync(string workerName, CancellationToken ct);
}
