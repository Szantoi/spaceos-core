using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDoorOrderRepository"/> backed by <see cref="JoineryDbContext"/>.
/// </summary>
public sealed class DoorOrderRepository(JoineryDbContext db) : IDoorOrderRepository
{
    /// <inheritdoc/>
    public async Task<DoorOrder?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
        => await db.DoorOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(DoorOrder order, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(order);
        await db.DoorOrders.AddAsync(order, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(DoorOrder order, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(order);
        // Entity is already tracked (loaded via GetByIdAsync without AsNoTracking).
        // Explicitly add any DoorItems not yet tracked so EF Core uses Added (not Modified).
        foreach (var item in order.Items)
        {
            if (db.Entry(item).State == EntityState.Detached)
                await db.Set<Domain.Entities.DoorItem>().AddAsync(item, ct).ConfigureAwait(false);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<DoorOrderDto> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.DoorOrders
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId);

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new DoorOrderDto(
                o.Id,
                o.TenantId,
                o.FlowEpicId,
                o.ProjectId,
                o.ProjectName ?? string.Empty,
                o.Status.ToString(),
                o.Items.Count,
                o.ProjectInfo != null ? o.ProjectInfo.DeliveryDate : null,
                default))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc/>
    public async Task<DoorOrder?> FindBySourceQuoteIdAsync(
        Guid tenantId, Guid sourceQuoteId, CancellationToken ct)
        => await db.DoorOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantId && o.SourceQuoteId == sourceQuoteId, ct)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<TenantDeletedCounts> DeleteAllByTenantAsync(Guid tenantId, CancellationToken ct)
    {
        // 1. Delete CuttingListSnapshots (load + remove — compatible with InMemory and PostgreSQL)
        var snapshots = await db.CuttingListSnapshots
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        db.CuttingListSnapshots.RemoveRange(snapshots);

        // 2. Delete ProductionSheetCaches
        var caches = await db.ProductionSheetCaches
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        db.ProductionSheetCaches.RemoveRange(caches);

        // 3. Delete JoineryOutboxEntries
        var outbox = await db.JoineryOutboxEntries
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        db.JoineryOutboxEntries.RemoveRange(outbox);

        // 4. Delete DoorOrders with their DoorItems (explicit — InMemory has no cascade)
        var orders = await db.DoorOrders
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var order in orders)
            db.Set<DoorItem>().RemoveRange(order.Items);

        db.DoorOrders.RemoveRange(orders);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new TenantDeletedCounts(orders.Count, snapshots.Count);
    }
}
