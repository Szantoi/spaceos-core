using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Orders.Repositories;

/// <summary>
/// Abstraction for persisting <see cref="JoineryOutboxEntry"/> records within the same
/// unit of work as the triggering command.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Adds outbox entries to the current unit of work. The caller is responsible for
    /// calling <c>SaveChangesAsync</c> (or equivalent) after adding entries.
    /// </summary>
    /// <param name="entries">Entries to enqueue.</param>
    void AddRange(IEnumerable<JoineryOutboxEntry> entries);

    /// <summary>
    /// Persists all pending changes including the enqueued outbox entries.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(CancellationToken ct);
}
