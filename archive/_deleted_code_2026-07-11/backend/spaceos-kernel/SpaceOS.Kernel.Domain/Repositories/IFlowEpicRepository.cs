using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="FlowEpic"/> aggregate root.
/// </summary>
/// <remarks>
/// All list/filter operations must be added as <c>ListAsync(ISpecification&lt;FlowEpic&gt;, CancellationToken)</c>
/// overloads using Ardalis.Specification — never raw LINQ in application handlers.
/// </remarks>
public interface IFlowEpicRepository
{
    /// <summary>Returns the <see cref="FlowEpic"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The epic identifier to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<FlowEpic?> GetByIdAsync(FlowEpicId id, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="FlowEpic"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="epic">The epic to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(FlowEpic epic, CancellationToken ct = default);

    /// <summary>Marks an existing <see cref="FlowEpic"/> as modified. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="epic">The epic to update.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task UpdateAsync(FlowEpic epic, CancellationToken ct = default);

    /// <summary>Returns all <see cref="FlowEpic"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<FlowEpic>> ListAsync(ISpecification<FlowEpic> specification, CancellationToken ct = default);

    /// <summary>Returns the total count of <see cref="FlowEpic"/> instances matching the given specification.</summary>
    /// <param name="specification">The filter specification (must not apply Skip/Take).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<FlowEpic> specification, CancellationToken ct = default);

    /// <summary>
    /// Deletes every <see cref="FlowEpic"/> owned by the specified tenant along with the
    /// related <see cref="Snapshots.AggregateSnapshot"/> and <see cref="AuditLog.AuditEvent"/>
    /// rows, and returns the number of rows deleted per table.
    /// </summary>
    /// <remarks>
    /// Reserved for test-infrastructure reset flows (BE-TEST-02). Callers must enforce
    /// an additional allowlist check (<c>TEST_TENANT_ALLOWLIST</c>) and the
    /// <c>X-SpaceOS-Internal</c> header gate before invoking this method.
    /// Deletion bypasses the tenant EF Core query filter — the <paramref name="tenantId"/>
    /// is used verbatim as the WHERE clause predicate.
    /// </remarks>
    /// <param name="tenantId">The tenant whose data should be erased.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<TenantDeletedCounts> DeleteAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
}

/// <summary>
/// Row-count summary returned by <see cref="IFlowEpicRepository.DeleteAllByTenantAsync"/>.
/// </summary>
/// <param name="FlowEpics">Number of <c>FlowEpics</c> rows removed.</param>
/// <param name="Snapshots">Number of <c>AggregateSnapshots</c> rows removed (FlowEpic state snapshots only).</param>
/// <param name="AuditEvents">Number of <c>AuditEvents</c> rows removed for the tenant.</param>
public readonly record struct TenantDeletedCounts(int FlowEpics, int Snapshots, int AuditEvents);
