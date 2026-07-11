// SpaceOS.Infrastructure/Data/Repositories/NodeManifestRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="INodeManifestRepository"/>.
/// </summary>
internal sealed class NodeManifestRepository : INodeManifestRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="NodeManifestRepository"/>.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public NodeManifestRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<NodeManifest?> GetByTenantIdAsync(TenantId tenantId, CancellationToken ct = default)
    {
        return await _context.NodeManifests
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.TenantId == tenantId, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(NodeManifest manifest, CancellationToken ct = default)
    {
        await _context.NodeManifests.AddAsync(manifest, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(NodeManifest manifest, CancellationToken ct = default)
    {
        _context.NodeManifests.Update(manifest);
        return Task.CompletedTask;
    }
}
