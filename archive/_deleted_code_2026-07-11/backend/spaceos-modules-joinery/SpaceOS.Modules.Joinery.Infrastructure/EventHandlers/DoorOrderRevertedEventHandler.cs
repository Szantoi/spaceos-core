using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.EventHandlers;

/// <summary>
/// SEC-03: When an order is reverted to Draft, invalidates all PDF cache files and
/// demotes all latest snapshots so they are no longer served.
/// </summary>
public sealed class DoorOrderRevertedEventHandler
    : INotificationHandler<DomainEventNotification<DoorOrderReverted>>
{
    private readonly JoineryDbContext _db;
    private readonly ILogger<DoorOrderRevertedEventHandler> _logger;

    public DoorOrderRevertedEventHandler(JoineryDbContext db, ILogger<DoorOrderRevertedEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<DoorOrderReverted> notification,
        CancellationToken ct)
    {
        var orderId = notification.DomainEvent.OrderId;

        // 1. Load all ProductionSheetCache entries linked to snapshots of this order
        var cacheEntries = await _db.ProductionSheetCaches
            .Join(
                _db.CuttingListSnapshots,
                c => c.SnapshotId,
                s => s.Id,
                (c, s) => new { Cache = c, Snapshot = s })
            .Where(x => x.Snapshot.DoorOrderId == orderId)
            .Select(x => x.Cache)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // 2. Delete PDF files from disk and remove cache DB records
        foreach (var entry in cacheEntries)
        {
            if (File.Exists(entry.FilePath))
            {
                File.Delete(entry.FilePath);
                _logger.LogInformation("Deleted PDF cache file {FilePath} for reverted order {OrderId}", entry.FilePath, orderId);
            }

            _db.ProductionSheetCaches.Remove(entry);
        }

        // 3. Demote all latest snapshots for this order (DB-03)
        var snapshots = await _db.CuttingListSnapshots
            .Where(s => s.DoorOrderId == orderId && s.IsLatest)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var snapshot in snapshots)
            snapshot.MarkNotLatest();

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Reverted order {OrderId}: invalidated {CacheCount} PDF cache(s) and {SnapshotCount} snapshot(s)",
            orderId, cacheEntries.Count, snapshots.Count);
    }
}
