// SpaceOS.Infrastructure/Persistence/UnitOfWork.cs

using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// Wraps <see cref="AppDbContext.SaveChangesAsync"/> as the single commit boundary.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    /// <summary>Initialises a new instance with the given <see cref="AppDbContext"/>.</summary>
    public UnitOfWork(AppDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
