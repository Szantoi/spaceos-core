// SpaceOS.Infrastructure/Data/Repositories/StageHandoffRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>EF Core implementation of <see cref="IStageHandoffRepository"/>.</summary>
internal sealed class StageHandoffRepository : IStageHandoffRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>Initialises a new <see cref="StageHandoffRepository"/>.</summary>
    /// <param name="dbContext">The application database context.</param>
    public StageHandoffRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<StageHandoff?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.StageHandoffs
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StageHandoff>> ListAsync(ISpecification<StageHandoff> spec, CancellationToken ct = default)
    {
        return await _dbContext.StageHandoffs
            .AsNoTracking()
            .WithSpecification(spec)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<StageHandoff?> FirstOrDefaultAsync(ISingleResultSpecification<StageHandoff> spec, CancellationToken ct = default)
    {
        return await _dbContext.StageHandoffs
            .AsNoTracking()
            .WithSpecification(spec)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(StageHandoff entity, CancellationToken ct = default)
    {
        await _dbContext.StageHandoffs.AddAsync(entity, ct).ConfigureAwait(false);
    }
}
