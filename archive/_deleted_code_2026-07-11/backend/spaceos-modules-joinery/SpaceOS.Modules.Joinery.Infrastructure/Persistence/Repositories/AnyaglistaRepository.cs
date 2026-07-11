using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Repositories;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAnyaglistaRepository"/>.
/// </summary>
public sealed class AnyaglistaRepository(JoineryDbContext db) : IAnyaglistaRepository
{
    public async Task<Domain.Core.Anyaglista?> GetByOrderIdAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken ct)
        => await db.Anyaglistak
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.OrderId == orderId && a.TenantId == tenantId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Domain.Core.Anyaglista anyaglista, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(anyaglista);
        await db.Anyaglistak.AddAsync(anyaglista, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
