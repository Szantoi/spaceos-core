using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFacilityRepository"/>.
/// </summary>
public class FacilityRepository : IFacilityRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="FacilityRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public FacilityRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<Facility?> GetByIdAsync(FacilityId id, CancellationToken ct = default)
    {
        return await _dbContext.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(TenantId tenantId, string name, CancellationToken ct = default)
    {
        var facilityName = FacilityName.From(name);
        return await _dbContext.Facilities.AsNoTracking().AnyAsync(f => f.TenantId == tenantId && f.Name == facilityName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Facility facility, CancellationToken ct = default)
    {
        await _dbContext.Facilities.AddAsync(facility, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Facility facility, CancellationToken ct = default)
    {
        _dbContext.Facilities.Update(facility);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Facility>> ListAsync(ISpecification<Facility> specification, CancellationToken ct = default)
    {
        return await _dbContext.Facilities
            .AsNoTracking()
            .WithSpecification(specification)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(ISpecification<Facility> specification, CancellationToken ct = default)
    {
        return await _dbContext.Facilities
            .AsNoTracking()
            .WithSpecification(specification)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
