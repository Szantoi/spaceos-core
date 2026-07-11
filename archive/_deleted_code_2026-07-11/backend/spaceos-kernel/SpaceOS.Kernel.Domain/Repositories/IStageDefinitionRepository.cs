// SpaceOS.Kernel.Domain/Repositories/IStageDefinitionRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>Persistence contract for the <see cref="StageDefinition"/> aggregate.</summary>
public interface IStageDefinitionRepository
{
    /// <summary>Returns the <see cref="StageDefinition"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    Task<StageDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the active <see cref="StageDefinition"/> for a tenant and stage code, or <see langword="null"/> if not found.</summary>
    Task<StageDefinition?> GetByCodeAsync(Guid tenantId, string stageCode, CancellationToken ct = default);

    /// <summary>Returns all <see cref="StageDefinition"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<StageDefinition>> ListAsync(ISpecification<StageDefinition> spec, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="StageDefinition"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    Task AddAsync(StageDefinition entity, CancellationToken ct = default);

    /// <summary>Marks an existing <see cref="StageDefinition"/> as modified. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    Task UpdateAsync(StageDefinition entity, CancellationToken ct = default);
}
