using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITenantRepository"/>.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initialises a new <see cref="TenantRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public TenantRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default)
    {
        return await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tenant>> ListAsync(ISpecification<Tenant> specification, CancellationToken ct = default)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .WithSpecification(specification)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await _dbContext.Tenants.AddAsync(tenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _dbContext.Tenants.Update(tenant);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(ISpecification<Tenant> specification, CancellationToken ct = default)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .WithSpecification(specification)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Tenant?> GetByEmailHashAsync(string emailHash, CancellationToken ct = default)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.EmailHash == emailHash && !t.IsArchived)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
