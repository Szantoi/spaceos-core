using SpaceOS.Modules.Sales.Application.Outbox;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Infrastructure.Outbox;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOutboxRepository"/>.</summary>
internal sealed class OutboxRepository(SalesDbContext db) : IOutboxRepository
{
    /// <inheritdoc/>
    public async Task AddMessageAsync(
        Guid tenantId,
        Guid aggregateId,
        string operation,
        string payloadJson,
        string idempotencyKey,
        IClock clock,
        CancellationToken ct)
    {
        var msg = OutboxMessage.Create(
            tenantId, aggregateId, operation, payloadJson, idempotencyKey, clock);
        await db.OutboxMessages.AddAsync(msg, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct).ConfigureAwait(false);
}
