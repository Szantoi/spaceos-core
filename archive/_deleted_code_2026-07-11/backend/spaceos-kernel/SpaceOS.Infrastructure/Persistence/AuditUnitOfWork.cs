// SpaceOS.Infrastructure/Persistence/AuditUnitOfWork.cs

using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAuditUnitOfWork"/>.
/// Wraps <see cref="AuditDbContext.SaveChangesAsync"/> to commit audit-log changes on the
/// isolated audit connection (role: <c>spaceos_audit_writer</c>).
/// </summary>
internal sealed class AuditUnitOfWork : IAuditUnitOfWork
{
    private readonly AuditDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="AuditUnitOfWork"/>.
    /// </summary>
    /// <param name="context">The audit-scoped database context.</param>
    public AuditUnitOfWork(AuditDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
