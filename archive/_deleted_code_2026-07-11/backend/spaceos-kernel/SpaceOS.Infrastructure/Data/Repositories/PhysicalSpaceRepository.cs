// SpaceOS.Infrastructure/Data/Repositories/PhysicalSpaceRepository.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Aggregates;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPhysicalSpaceRepository"/>.
/// </summary>
public class PhysicalSpaceRepository : IPhysicalSpaceRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="PhysicalSpaceRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public PhysicalSpaceRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<PhysicalSpace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.PhysicalSpaces
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.PhysicalSpaces
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(PhysicalSpace space, CancellationToken ct = default)
    {
        await _dbContext.PhysicalSpaces.AddAsync(space, ct).ConfigureAwait(false);
    }
}
