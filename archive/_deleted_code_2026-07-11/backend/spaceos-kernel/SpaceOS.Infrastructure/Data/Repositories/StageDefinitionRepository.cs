// SpaceOS.Infrastructure/Data/Repositories/StageDefinitionRepository.cs
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

/// <summary>EF Core implementation of <see cref="IStageDefinitionRepository"/>.</summary>
internal sealed class StageDefinitionRepository : IStageDefinitionRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>Initialises a new <see cref="StageDefinitionRepository"/>.</summary>
    /// <param name="dbContext">The application database context.</param>
    public StageDefinitionRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<StageDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.StageDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<StageDefinition?> GetByCodeAsync(Guid tenantId, string stageCode, CancellationToken ct = default)
    {
        return await _dbContext.StageDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.TenantId == tenantId && sd.StageCode == stageCode, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StageDefinition>> ListAsync(ISpecification<StageDefinition> spec, CancellationToken ct = default)
    {
        return await _dbContext.StageDefinitions
            .AsNoTracking()
            .WithSpecification(spec)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(StageDefinition entity, CancellationToken ct = default)
    {
        await _dbContext.StageDefinitions.AddAsync(entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(StageDefinition entity, CancellationToken ct = default)
    {
        _dbContext.StageDefinitions.Update(entity);
        return Task.CompletedTask;
    }
}
