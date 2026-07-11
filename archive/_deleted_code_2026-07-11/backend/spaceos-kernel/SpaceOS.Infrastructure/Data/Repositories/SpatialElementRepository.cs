// SpaceOS.Infrastructure/Data/Repositories/SpatialElementRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISpatialElementRepository"/>.
/// </summary>
public class SpatialElementRepository : ISpatialElementRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="SpatialElementRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public SpatialElementRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<SpatialElement?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.SpatialElements
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(SpatialElement element, CancellationToken ct = default)
    {
        await _dbContext.SpatialElements.AddAsync(element, ct).ConfigureAwait(false);
    }
}
