using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISpaceLayerRepository"/>.
/// </summary>
public class SpaceLayerRepository : ISpaceLayerRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="SpaceLayerRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public SpaceLayerRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<SpaceLayer?> GetByIdAsync(SpaceLayerId id, CancellationToken ct = default)
    {
        return await _dbContext.SpaceLayers.AsNoTracking().FirstOrDefaultAsync(sl => sl.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(SpaceLayer layer, CancellationToken ct = default)
    {
        await _dbContext.SpaceLayers.AddAsync(layer, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(SpaceLayer spaceLayer, CancellationToken ct = default)
    {
        _dbContext.SpaceLayers.Update(spaceLayer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SpaceLayer>> ListAsync(ISpecification<SpaceLayer> specification, CancellationToken ct = default)
    {
        return await _dbContext.SpaceLayers
            .AsNoTracking()
            .WithSpecification(specification)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(ISpecification<SpaceLayer> specification, CancellationToken ct = default)
    {
        return await _dbContext.SpaceLayers
            .AsNoTracking()
            .WithSpecification(specification)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
