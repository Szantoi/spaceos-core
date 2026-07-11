// SpaceOS.Infrastructure/Data/Repositories/OutboxRepository.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Outbox;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOutboxRepository"/>.
/// </summary>
internal sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="OutboxRepository"/>.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public OutboxRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _context.OutboxMessages.AddAsync(message, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(OutboxMessage message, CancellationToken ct = default)
    {
        _context.OutboxMessages.Update(message);
        return Task.CompletedTask;
    }
}
