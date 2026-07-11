// SpaceOS.Infrastructure/Data/Repositories/StageChainTemplateRepository.cs
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

/// <summary>EF Core implementation of <see cref="IStageChainTemplateRepository"/>.</summary>
internal sealed class StageChainTemplateRepository : IStageChainTemplateRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>Initialises a new <see cref="StageChainTemplateRepository"/>.</summary>
    /// <param name="dbContext">The application database context.</param>
    public StageChainTemplateRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<StageChainTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.StageChainTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<StageChainTemplate?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.StageChainTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StageChainTemplate>> ListAsync(ISpecification<StageChainTemplate> spec, CancellationToken ct = default)
    {
        return await _dbContext.StageChainTemplates
            .AsNoTracking()
            .WithSpecification(spec)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(StageChainTemplate entity, CancellationToken ct = default)
    {
        await _dbContext.StageChainTemplates.AddAsync(entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(StageChainTemplate entity, CancellationToken ct = default)
    {
        _dbContext.StageChainTemplates.Update(entity);
        return Task.CompletedTask;
    }
}
