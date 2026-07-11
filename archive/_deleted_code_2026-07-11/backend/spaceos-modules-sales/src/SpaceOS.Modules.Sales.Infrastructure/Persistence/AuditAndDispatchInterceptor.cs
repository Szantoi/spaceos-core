using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence;

/// <summary>
/// EF Core SaveChanges interceptor that:
/// 1. Writes audit entries for every domain event (SEC-S-08, same transaction).
/// 2. Publishes domain events via MediatR for notification/read-model handlers.
/// BE-S-02: outbox writes are the handler's explicit responsibility; this interceptor is
/// audit + notification ONLY.
/// </summary>
public sealed class AuditAndDispatchInterceptor(
    IPublisher mediator,
    ITenantContext tenant,
    IClock clock) : SaveChangesInterceptor
{
    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var ctx = eventData.Context!;
        var aggregates = ctx.ChangeTracker.Entries<TenantScopedEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var agg in aggregates)
        {
            foreach (var evt in agg.DomainEvents)
            {
                // SEC-S-08: audit entry written in the same transaction as the aggregate
                ctx.Set<AuditEntry>().Add(
                    AuditEntry.From(evt, tenant.TenantId, tenant.ActorSub, clock));

                // Publish for notification handlers (read-model projections, etc.)
                await mediator.Publish(evt, ct).ConfigureAwait(false);
            }
        }

        foreach (var agg in aggregates)
            agg.ClearDomainEvents();

        return result;
    }
}
