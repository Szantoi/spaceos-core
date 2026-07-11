// Identity.Infrastructure/Persistence/Repositories/KcSyncOutboxRepository.cs

using Identity.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class KcSyncOutboxRepository : IKcSyncOutboxRepository, IKcSyncOutboxProcessor
{
    private readonly IdentityDbContext _db;

    public KcSyncOutboxRepository(IdentityDbContext db) => _db = db;

    public async Task InsertAsync(KcSyncOutboxEntry entry, CancellationToken ct = default)
    {
        await _db.KcSyncOutbox.AddAsync(entry, ct).ConfigureAwait(false);
        // SaveChanges called by outer Unit of Work (repository AddAsync transaction)
        // If called standalone, caller is responsible for SaveChanges
    }

    public async Task<IReadOnlyList<KcSyncOutboxEntry>> GetPendingAsync(
        int maxAttempts, int limit, CancellationToken ct = default)
    {
        return await _db.KcSyncOutbox
            .AsNoTracking()
            .Where(e => EF.Property<DateTimeOffset?>(e, "ProcessedAt") == null
                        && e.AttemptCount < maxAttempts)
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid entryId, CancellationToken ct = default)
    {
        await _db.KcSyncOutbox
            .Where(e => e.Id == entryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => EF.Property<DateTimeOffset?>(e, "ProcessedAt"), DateTimeOffset.UtcNow),
                ct)
            .ConfigureAwait(false);
    }

    public async Task IncrementAttemptAsync(Guid entryId, CancellationToken ct = default)
    {
        await _db.KcSyncOutbox
            .Where(e => e.Id == entryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                .SetProperty(e => EF.Property<DateTimeOffset?>(e, "LastAttemptAt"), DateTimeOffset.UtcNow),
                ct)
            .ConfigureAwait(false);
    }
}
