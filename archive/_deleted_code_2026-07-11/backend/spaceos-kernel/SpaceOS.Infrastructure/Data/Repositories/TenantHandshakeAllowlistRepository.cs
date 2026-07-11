// SpaceOS.Infrastructure/Data/Repositories/TenantHandshakeAllowlistRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.DTOs;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITenantHandshakeAllowlistRepository"/>.
/// </summary>
internal sealed class TenantHandshakeAllowlistRepository : ITenantHandshakeAllowlistRepository
{
    private readonly AppDbContext _context;

    /// <summary>Initialises the repository.</summary>
    public TenantHandshakeAllowlistRepository(AppDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AllowedHostDto>> GetAllowedHostsAsync(
        Guid guestTenantId, CancellationToken ct = default)
    {
        var results = await _context.TenantHandshakeAllowlists
            .AsNoTracking()
            .Where(a => a.GuestTenantId == guestTenantId)
            .Join(
                _context.Tenants.IgnoreQueryFilters(),
                a => a.HostTenantId,
                t => t.Id.Value,
                (a, t) => new AllowedHostDto(a.HostTenantId, t.Name.Value, a.AllowedTradeTypes))
            .Take(20)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return results.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAllowedAsync(
        Guid guestTenantId, Guid hostTenantId, CancellationToken ct = default)
    {
        return await _context.TenantHandshakeAllowlists
            .AsNoTracking()
            .AnyAsync(
                a => a.GuestTenantId == guestTenantId && a.HostTenantId == hostTenantId,
                ct)
            .ConfigureAwait(false);
    }
}
