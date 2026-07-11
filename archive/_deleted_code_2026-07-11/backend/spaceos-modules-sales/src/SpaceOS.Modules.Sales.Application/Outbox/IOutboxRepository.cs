using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Application.Outbox;

/// <summary>
/// Repository contract for the transactional outbox. ADR-039 / D-11.
/// Used by the Application layer to enqueue outbox messages in the same transaction as aggregate mutations.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>Creates and enqueues a new outbox message in the current unit of work.</summary>
    Task AddMessageAsync(
        Guid tenantId,
        Guid aggregateId,
        string operation,
        string payloadJson,
        string idempotencyKey,
        IClock clock,
        CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
