using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGyartasilapRepository"/> backed by <see cref="JoineryDbContext"/>.
/// </summary>
public sealed class GyartasilapRepository(JoineryDbContext db) : IGyartasilapRepository
{
    public async Task<Gyartasilap?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
        => await db.Gyartasilaps
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Gyartasilap gyartasilap, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(gyartasilap);
        await db.Gyartasilaps.AddAsync(gyartasilap, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Gyartasilap gyartasilap, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(gyartasilap);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Gyartasilap>> ListByOrderAsync(
        Guid joineryOrderId,
        Guid tenantId,
        GyartasilapStatus? status,
        CancellationToken ct)
    {
        var query = db.Gyartasilaps
            .AsNoTracking()
            .Where(g => g.JoineryOrderId == joineryOrderId && g.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
