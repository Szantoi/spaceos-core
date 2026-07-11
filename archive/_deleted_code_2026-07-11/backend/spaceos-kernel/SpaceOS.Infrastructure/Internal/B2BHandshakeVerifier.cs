using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Application.Internal.Ports;

namespace SpaceOS.Infrastructure.Internal;

/// <summary>
/// EF Core implementation of <see cref="IB2BHandshakeVerifier"/>.
/// Queries the <c>TenantHandshakeAllowlists</c> table in both directions (ADR-039).
/// </summary>
internal sealed class B2BHandshakeVerifier(AppDbContext db) : IB2BHandshakeVerifier
{
    /// <inheritdoc/>
    public async Task<bool> HasVerifiedHandshakeAsync(
        Guid tenantA,
        Guid tenantB,
        CancellationToken ct)
        => await db.TenantHandshakeAllowlists
            .AsNoTracking()
            .AnyAsync(
                h => (h.GuestTenantId == tenantA && h.HostTenantId == tenantB)
                  || (h.GuestTenantId == tenantB && h.HostTenantId == tenantA),
                ct)
            .ConfigureAwait(false);
}
