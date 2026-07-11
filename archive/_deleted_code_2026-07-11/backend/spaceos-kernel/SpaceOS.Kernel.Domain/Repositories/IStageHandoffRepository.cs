// SpaceOS.Kernel.Domain/Repositories/IStageHandoffRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>Persistence contract for the <see cref="StageHandoff"/> aggregate.</summary>
public interface IStageHandoffRepository
{
    /// <summary>Returns the <see cref="StageHandoff"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    Task<StageHandoff?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all <see cref="StageHandoff"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<StageHandoff>> ListAsync(ISpecification<StageHandoff> spec, CancellationToken ct = default);

    /// <summary>Returns the first <see cref="StageHandoff"/> matching the given single-result specification, or <see langword="null"/>.</summary>
    Task<StageHandoff?> FirstOrDefaultAsync(ISingleResultSpecification<StageHandoff> spec, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="StageHandoff"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    Task AddAsync(StageHandoff entity, CancellationToken ct = default);
}
