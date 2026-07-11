// SpaceOS.Infrastructure/Data/Repositories/SpatialTaskLinkRepository.cs
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISpatialTaskLinkRepository"/>.
/// </summary>
public class SpatialTaskLinkRepository : ISpatialTaskLinkRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="SpatialTaskLinkRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public SpatialTaskLinkRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task AddAsync(SpatialTaskLink link, CancellationToken ct = default)
    {
        await _dbContext.SpatialTaskLinks.AddAsync(link, ct).ConfigureAwait(false);
    }
}
