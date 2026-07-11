using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWorkStationRepository"/>.
/// </summary>
public class WorkStationRepository : IWorkStationRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="WorkStationRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public WorkStationRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<WorkStation?> GetByIdAsync(WorkStationId id, CancellationToken ct = default)
    {
        return await _dbContext.WorkStations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(FacilityId facilityId, string name, CancellationToken ct = default)
    {
        var wsName = WorkStationName.From(name);
        return await _dbContext.WorkStations
            .AsNoTracking()
            .AnyAsync(w => w.FacilityId == facilityId && w.Name == wsName, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(WorkStation workStation, CancellationToken ct = default)
    {
        await _dbContext.WorkStations.AddAsync(workStation, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(WorkStation workStation, CancellationToken ct = default)
    {
        _dbContext.WorkStations.Update(workStation);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkStation>> ListAsync(ISpecification<WorkStation> specification, CancellationToken ct = default)
    {
        return await _dbContext.WorkStations
            .AsNoTracking()
            .WithSpecification(specification)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(ISpecification<WorkStation> specification, CancellationToken ct = default)
    {
        return await _dbContext.WorkStations
            .AsNoTracking()
            .WithSpecification(specification)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
