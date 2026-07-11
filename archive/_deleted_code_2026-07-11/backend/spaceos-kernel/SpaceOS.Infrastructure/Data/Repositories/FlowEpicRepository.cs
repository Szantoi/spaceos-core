using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFlowEpicRepository"/>.
/// </summary>
public class FlowEpicRepository : IFlowEpicRepository
{
    private readonly AppDbContext _dbContext;
    private readonly AuditDbContext? _auditDbContext;

    /// <summary>
    /// Initialises a new <see cref="FlowEpicRepository"/>.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="auditDbContext">
    /// The audit database context (optional). Required only for
    /// <see cref="DeleteAllByTenantAsync"/>; may be <see langword="null"/>
    /// when only standard CRUD operations are needed (e.g. in repository-level tests).
    /// </param>
    public FlowEpicRepository(AppDbContext dbContext, AuditDbContext? auditDbContext = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _auditDbContext = auditDbContext;
    }

    /// <inheritdoc/>
    public async Task<FlowEpic?> GetByIdAsync(FlowEpicId id, CancellationToken ct = default)
    {
        return await _dbContext.FlowEpics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(FlowEpic epic, CancellationToken ct = default)
    {
        await _dbContext.FlowEpics.AddAsync(epic, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(FlowEpic epic, CancellationToken ct = default)
    {
        _dbContext.FlowEpics.Update(epic);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FlowEpic>> ListAsync(ISpecification<FlowEpic> specification, CancellationToken ct = default)
    {
        return await _dbContext.FlowEpics
            .AsNoTracking()
            .WithSpecification(specification)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(ISpecification<FlowEpic> specification, CancellationToken ct = default)
    {
        return await _dbContext.FlowEpics
            .AsNoTracking()
            .WithSpecification(specification)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <c>IgnoreQueryFilters</c> so tenant/archive filters do not hide rows.
    /// EF Core <c>ExecuteDeleteAsync</c> (bulk delete) is used for performance and
    /// to bypass change-tracker overhead on potentially large data sets.
    /// Snapshots are filtered to <c>AggregateType == "FlowEpic"</c> so only
    /// FlowEpic state snapshots are removed — other aggregate types stay untouched.
    /// </remarks>
    public async Task<TenantDeletedCounts> DeleteAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        // 1. Delete AggregateSnapshots for FlowEpics owned by this tenant.
        var snapshotCount = await _dbContext.AggregateSnapshots
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.AggregateType == nameof(FlowEpic))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        // 2. Delete FlowEpics owned by this tenant.
        var tenantIdVo = TenantId.From(tenantId);
        var epicCount = await _dbContext.FlowEpics
            .IgnoreQueryFilters()
            .Where(f => f.TenantId == tenantIdVo)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        // 3. Delete AuditEvents for this tenant (separate DbContext).
        var auditCount = 0;
        if (_auditDbContext is not null)
        {
            auditCount = await _auditDbContext.AuditEvents
                .Where(ae => ae.TenantId == tenantId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);
        }

        return new TenantDeletedCounts(epicCount, snapshotCount, auditCount);
    }
}
