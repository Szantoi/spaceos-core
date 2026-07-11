// SpaceOS.Infrastructure/Data/Repositories/SyncSignalRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISyncSignalRepository"/>.
/// Append-only — signals are never updated after insertion (only <see cref="SyncSignal.MarkSynced"/> state is persisted via the handler).
/// </summary>
internal sealed class SyncSignalRepository : ISyncSignalRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="SyncSignalRepository"/>.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public SyncSignalRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<SyncSignal?> GetByClientSignalIdAsync(
        TenantId tenantId,
        Guid clientSignalId,
        CancellationToken ct = default)
    {
        return await _context.SyncSignals
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.ClientSignalId == clientSignalId,
                ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(SyncSignal signal, CancellationToken ct = default)
    {
        await _context.SyncSignals.AddAsync(signal, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> GetLastHashAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var last = await _context.SyncSignals
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.OccurredAt)
            .Select(s => s.StateHash)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return last ?? SyncConstants.GenesisHash;
    }
}
