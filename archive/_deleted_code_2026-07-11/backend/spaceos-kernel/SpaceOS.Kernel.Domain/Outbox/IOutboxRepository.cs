// SpaceOS.Kernel.Domain/Outbox/IOutboxRepository.cs

namespace SpaceOS.Kernel.Domain.Outbox;

/// <summary>
/// Repository abstraction for the transactional outbox.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Appends a new outbox message to the store.
    /// </summary>
    /// <param name="message">The message to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);

    /// <summary>
    /// Returns up to <paramref name="batchSize"/> messages in <see cref="OutboxStatus.Pending"/> status,
    /// ordered by creation time ascending.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Returns up to <paramref name="batchSize"/> unprocessed messages ordered by creation time ascending.
    /// </summary>
    /// <remarks>
    /// Backward-compatible alias that queries by <see cref="OutboxStatus.Pending"/>.
    /// Prefer <see cref="GetPendingAsync"/> in new code.
    /// </remarks>
    /// <param name="batchSize">The maximum number of messages to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Persists changes to an existing <see cref="OutboxMessage"/> (e.g. after calling <see cref="OutboxMessage.MarkProcessed"/>).
    /// </summary>
    /// <param name="message">The message to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(OutboxMessage message, CancellationToken ct = default);
}
