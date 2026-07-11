using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOutboxWriter"/> that writes outbox entries
/// into the same <see cref="JoineryDbContext"/> unit of work as the calling command handler.
/// </summary>
public sealed class OutboxWriter(JoineryDbContext db) : IOutboxWriter
{
    /// <inheritdoc/>
    public void AddRange(IEnumerable<JoineryOutboxEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        db.JoineryOutboxEntries.AddRange(entries);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct).ConfigureAwait(false);
}
