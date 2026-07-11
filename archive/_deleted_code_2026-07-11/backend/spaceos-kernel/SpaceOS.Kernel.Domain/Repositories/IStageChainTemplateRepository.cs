// SpaceOS.Kernel.Domain/Repositories/IStageChainTemplateRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>Persistence contract for the <see cref="StageChainTemplate"/> aggregate.</summary>
public interface IStageChainTemplateRepository
{
    /// <summary>Returns the <see cref="StageChainTemplate"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    Task<StageChainTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the <see cref="StageChainTemplate"/> with its <see cref="StageChainTemplate.Steps"/> navigation loaded,
    /// or <see langword="null"/> if not found.
    /// </summary>
    Task<StageChainTemplate?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all <see cref="StageChainTemplate"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<StageChainTemplate>> ListAsync(ISpecification<StageChainTemplate> spec, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="StageChainTemplate"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    Task AddAsync(StageChainTemplate entity, CancellationToken ct = default);

    /// <summary>Marks an existing <see cref="StageChainTemplate"/> as modified. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    Task UpdateAsync(StageChainTemplate entity, CancellationToken ct = default);
}
