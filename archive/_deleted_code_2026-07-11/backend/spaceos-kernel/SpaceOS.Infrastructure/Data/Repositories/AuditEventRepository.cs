// SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs

using Ardalis.Specification.EntityFrameworkCore;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuditEventRepository"/>.
/// Append-only — no update or delete operations are exposed.
/// Uses <see cref="AuditDbContext"/> so all queries and writes flow through the
/// <c>spaceos_audit_writer</c> PostgreSQL role with row-level security enforced.
/// </summary>
internal sealed class AuditEventRepository : IAuditEventRepository
{
    private readonly AuditDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="AuditEventRepository"/>.
    /// </summary>
    /// <param name="context">The audit-scoped database context.</param>
    public AuditEventRepository(AuditDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        await _context.AuditEvents.AddAsync(auditEvent, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditEvent>> ListAsync(ISpecification<AuditEvent> specification, CancellationToken ct = default)
    {
        return await _context.AuditEvents
            .AsNoTracking()
            .WithSpecification(specification)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(ISpecification<AuditEvent> specification, CancellationToken ct = default)
    {
        return await _context.AuditEvents
            .AsNoTracking()
            .WithSpecification(specification)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> GetLastHashAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Note: ORDER BY DateTimeOffset is not portable across SQLite and PostgreSQL in EF Core 8.
        // Load all hashes for the tenant, then sort client-side.
        // This is called under an advisory lock, so the dataset is bounded by the lock window.
        var rows = await _context.AuditEvents
            .AsNoTracking()
            .Where(ae => ae.TenantId == tenantId)
            .Select(ae => new { ae.StateHash, ae.OccurredAt, ae.Sequence })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .OrderByDescending(r => r.OccurredAt)
            .ThenByDescending(r => r.Sequence)
            .Select(r => r.StateHash)
            .FirstOrDefault() ?? "GENESIS";
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditEvent>> GetChainAsync(
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var query = _context.AuditEvents
            .AsNoTracking()
            .Where(ae => ae.TenantId == tenantId);

        if (from.HasValue)
            query = query.Where(ae => ae.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(ae => ae.OccurredAt <= to.Value);

        // ORDER BY DateTimeOffset is not portable in EF Core 8 SQLite; sort client-side.
        // Sequence is a BIGINT GENERATED ALWAYS AS IDENTITY tiebreaker (PostgreSQL).
        // In SQLite test environments Sequence is always 0 — OccurredAt alone orders correctly.
        var events = await query.ToListAsync(ct).ConfigureAwait(false);
        return events.OrderBy(e => e.OccurredAt).ThenBy(e => e.Sequence).ToList();
    }
}
