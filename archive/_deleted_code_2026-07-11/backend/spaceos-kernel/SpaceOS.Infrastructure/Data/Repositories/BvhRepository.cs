// SpaceOS.Infrastructure/Data/Repositories/BvhRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBvhRepository"/>.
/// </summary>
public class BvhRepository : IBvhRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="BvhRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public BvhRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<BvhNode?> GetRootAsync(Guid physicalSpaceId, CancellationToken ct = default)
    {
        return await _dbContext.BvhNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.PhysicalSpaceId == physicalSpaceId && n.ParentId == null, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BvhNode>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
    {
        return await _dbContext.BvhNodes
            .AsNoTracking()
            .Where(n => n.ParentId == parentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(BvhNode node, CancellationToken ct = default)
    {
        await _dbContext.BvhNodes.AddAsync(node, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> GetIntersectingLeafElementIdsAsync(
        Guid physicalSpaceId, BoundingBox query, CancellationToken ct = default)
    {
        return await _dbContext.BvhNodes
            .AsNoTracking()
            .Where(n => n.PhysicalSpaceId == physicalSpaceId
                && n.IsLeaf
                && n.ElementId != null
                && n.BoundingBox.MinX <= query.MaxX && n.BoundingBox.MaxX >= query.MinX
                && n.BoundingBox.MinY <= query.MaxY && n.BoundingBox.MaxY >= query.MinY
                && n.BoundingBox.MinZ <= query.MaxZ && n.BoundingBox.MaxZ >= query.MinZ)
            .Select(n => n.ElementId!.Value)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
