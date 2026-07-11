// Identity.Infrastructure/Persistence/Repositories/SpaceOSUserRepository.cs

using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class SpaceOSUserRepository : ISpaceOSUserRepository
{
    private readonly IdentityDbContext _db;

    public SpaceOSUserRepository(IdentityDbContext db) => _db = db;

    public async Task<SpaceOSUser?> GetByIdAsync(SpaceOSUserId id, CancellationToken ct = default)
    {
        return await _db.SpaceOSUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<SpaceOSUser?> GetByEmailAsync(Email email, Guid tenantId, CancellationToken ct = default)
    {
        return await _db.SpaceOSUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenantId, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SpaceOSUser>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.SpaceOSUsers
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(SpaceOSUser user, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        // DB-05: SET LOCAL az RLS-hez
        await _db.SetTenantContextAsync(user.TenantId, ct).ConfigureAwait(false);

        await _db.SpaceOSUsers.AddAsync(user, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SpaceOSUser user, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        // DB-05: SET LOCAL az RLS-hez
        await _db.SetTenantContextAsync(user.TenantId, ct).ConfigureAwait(false);

        _db.SpaceOSUsers.Update(user);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await tx.CommitAsync(ct).ConfigureAwait(false);
    }
}
