using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGyartasilapBatchRepository"/>.
/// </summary>
public sealed class GyartasilapBatchRepository(JoineryDbContext db) : IGyartasilapBatchRepository
{
    public async Task<GyartasilapBatch?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
        => await db.GyartasilapBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(GyartasilapBatch batch, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(batch);
        await db.GyartasilapBatches.AddAsync(batch, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GyartasilapBatch batch, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(batch);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
